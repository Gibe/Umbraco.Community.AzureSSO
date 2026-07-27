# Support Azure App Service Easy Auth (Issue #57)

## Context

[Issue #57](https://github.com/Gibe/Umbraco.Community.AzureSSO/issues/57) asks for support logging into the Umbraco backoffice when the site is deployed to Azure App Service with **Easy Auth** (platform-level authentication) turned on for the AAD provider. Today, enabling Easy Auth breaks backoffice login entirely.

I traced the actual root cause by reading the real source of `Microsoft.Identity.Web` (this package's dependency) and `Umbraco-CMS` (the two libraries whose interaction matters here), rather than guessing:

**Root cause**: `MicrosoftAccountAuthenticationExtensions.cs:58` calls `AddMicrosoftIdentityWebApp(..., cookieScheme: $"{profile.Name}Cookies", openIdConnectScheme: SchemeForBackOffice(profile.Name, ...))` once per enabled profile. Internally, Microsoft.Identity.Web checks `AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled` (true when the App Service env vars `WEBSITE_AUTH_ENABLED=True` and `WEBSITE_AUTH_DEFAULT_PROVIDER=AzureActiveDirectory|AAD` are present — i.e. Easy Auth is on). When true, it **ignores the `openIdConnectScheme`/`cookieScheme` parameters completely** and instead does:
```csharp
builder.Services.AddAuthentication(AppServicesAuthenticationDefaults.AuthenticationScheme)
    .AddAppServicesAuthentication();
```
`AddAppServicesAuthentication()` is hardcoded to register its handler under the fixed scheme name `"AppServicesAuthentication"` — never under the per-profile scheme name Umbraco's backoffice `Challenge()`/`SignIn()` actually targets. Result: no handler exists under the expected scheme name → login fails outright (and with >1 enabled profile, the second call throws "Scheme already exists").

Even ignoring the scheme-name mismatch, Microsoft.Identity.Web's `AppServicesAuthenticationHandler` only implements `HandleAuthenticateAsync` (a passive per-request header read of `X-MS-TOKEN-AAD-ID-TOKEN`/`X-MS-CLIENT-PRINCIPAL-IDP`) — it has no challenge/redirect/callback behaviour, so it can never be driven through Umbraco's expected "click login button → redirect → provider → callback → auto-link" flow even if the scheme name matched.

**The fix is not "make Microsoft.Identity.Web's Easy Auth path work"** — it can't, by design, integrate with Umbraco's backoffice external-login machinery. Instead, this package should implement its **own** minimal remote-authentication handler that:
- Is challenged by redirecting to App Service's built-in `/.auth/login/aad` (which performs the real AAD sign-in and sets the App Service session).
- Completes when the browser lands back on our own callback path, at which point App Service is already attaching the `X-MS-TOKEN-AAD-ID-TOKEN` header to every request — we just read it there.

I confirmed via `Umbraco.Cms.Web.BackOffice.Security.BackOfficeAuthenticationBuilder` (net6-8) and `Umbraco.Cms.Api.Management.Security.BackOfficeAuthenticationBuilder` (net9-10) source — both override **only** `AddRemoteScheme<TOptions, THandler>() where TOptions : RemoteAuthenticationOptions`, and that override is what registers the scheme in Umbraco's `BackOfficeExternalLoginProvider` registry (making the login button appear) and force-sets `options.SignInScheme` to Umbraco's backoffice external cookie. Plain `AddScheme` bypasses all of that. So the new handler must derive from `RemoteAuthenticationHandler<TOptions>` and be registered via `AddRemoteScheme`, exactly like OIDC is today — this is fully supported by both backoffice generations, requires **zero changes** to the existing claims-mapping/auto-link code (`MicrosoftAccountBackOfficeExternalLoginProviderOptions.cs`), since that code already only depends on `loginInfo.Principal` (a plain `ClaimsPrincipal`), not on anything OIDC-specific.

## Design

### New files — `src/Umbraco.Community.AzureSSO/EasyAuth/`

**`EasyAuthDetection.cs`** — thin static wrapper:
```csharp
public static class EasyAuthDetection
{
    public static bool IsEnabled => AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled;
}
```

**`EasyAuthAuthenticationOptions.cs`** — `class EasyAuthAuthenticationOptions : RemoteAuthenticationOptions { }` (uses the inherited `CallbackPath`/`SignInScheme`; `SignInScheme` gets force-set by Umbraco's `EnsureBackOfficeScheme` post-configure, same as OIDC today).

**`EasyAuthAuthenticationHandler.cs`** — `class EasyAuthAuthenticationHandler : RemoteAuthenticationHandler<EasyAuthAuthenticationOptions>`:
- Constructor: mirror the exact `#if NET8_0_OR_GREATER` / else split already used by Microsoft.Identity.Web's own `AppServicesAuthenticationHandler` (3-arg ctor on net8+, 4-arg with `ISystemClock` on net6/net7), since this project targets both.
- `HandleRemoteAuthenticateAsync()`: if `!EasyAuthDetection.IsEnabled`, return `HandleRequestResult.Fail("Easy Auth is not active on this host")`. Otherwise call `AppServicesAuthenticationInformation.GetUser(Context.Request.Headers)`; if `null`, fail (App Service hasn't attached the headers yet — shouldn't normally happen on this path). If the resulting `ClaimsPrincipal` has no `ClaimTypes.NameIdentifier` claim (the AAD ID token carries `oid`/`sub`, not that URI), add one copied from the `ClaimConstants.Oid` (or `Sub`) claim — ASP.NET Core Identity's `SignInManager.GetExternalLoginInfoAsync()` reads `ClaimTypes.NameIdentifier` as the external provider key, so without this the user can never be linked. Build an `AuthenticationTicket(principal, properties, Scheme.Name)` with `properties.RedirectUri` restored from a `returnUrl` query string param (see challenge, below) and return `HandleRequestResult.Success(ticket)`.
- `HandleChallengeAsync(AuthenticationProperties properties)`: redirect to `/.auth/login/aad?post_login_redirect_uri=<our own CallbackPath, with the real return URL appended as a `returnUrl` query param>`. This is necessary because Easy Auth's own login round-trip has no concept of carrying arbitrary `AuthenticationProperties`/state — we thread the eventual return URL through as a plain query string on our callback path instead.
- Also implement `IAuthenticationSignOutHandler.SignOutAsync(AuthenticationProperties? properties)` (the same interface `OpenIdConnectHandler` implements alongside `RemoteAuthenticationHandler`, confirmed from ASP.NET Core source): redirect to `AppServicesAuthenticationInformation.LogoutUrl` (defaults to `/.auth/logout`) with `post_logout_redirect_uri` pointing at the profile's existing `SignedOutCallbackPath` setting. Umbraco's backoffice logout already calls `SignOutAsync` against every linked external provider's scheme generically (this is how it works for OIDC today via `SignedOutCallbackPath`) — implementing this interface is enough for our scheme to be included in that same generic flow, no new Umbraco-side hook needed. This fully ends the Easy Auth/App-Service session on logout, not just the local Umbraco cookie — otherwise the user would be silently signed back in on their next visit since the App Service session cookie would still be valid.

No middleware, no `UmbracoPipelineOptions`, no bypassing `ExternalSignInAutoLinkOptions` — once `HandleRequestAsync` (inherited, unmodified, from `RemoteAuthenticationHandler<TOptions>`) matches our `CallbackPath`, it calls `HandleRemoteAuthenticateAsync()`, signs the resulting principal into `SignInScheme` (Umbraco's backoffice external cookie), and redirects — from there Umbraco's existing `SignInManager.GetExternalLoginInfoAsync()` → `OnAutoLinking`/`OnExternalLogin` → `SetGroups`/`SetName` runs completely unchanged.

### `MicrosoftAccountAuthenticationExtensions.cs`

Compute `var easyAuthActive = EasyAuthDetection.IsEnabled;` once, before the profile loop. If active and more than one profile is `Enabled`, throw a clear `InvalidOperationException` at startup (Easy Auth is one site-wide identity — it cannot be split across multiple tenants/profiles the way the OIDC flow can).

Inside the loop, branch:
```csharp
if (easyAuthActive)
{
    backOfficeAuthenticationBuilder.AddRemoteScheme<EasyAuthAuthenticationOptions, EasyAuthAuthenticationHandler>(
        SchemeForBackOffice(profile.Name, backOfficeAuthenticationBuilder),
        profile.DisplayName ?? "Microsoft Entra ID",
        options => options.CallbackPath = profile.Credentials.CallbackPath);
}
else
{
    // existing AddMicrosoftIdentityWebApp(...).EnableTokenAcquisitionToCallDownstreamApi(...).AddTokenCaches(...) — unchanged
}
```
The Easy Auth branch reuses the existing `Credentials.CallbackPath` setting (it's just our own dispatch path now, not an OIDC redirect_uri) — no new required config field. `EnableTokenAcquisitionToCallDownstreamApi`/token caches are skipped for this branch (not chainable off `AddRemoteScheme`'s plain `AuthenticationBuilder` return type anyway — downstream Graph calls via the App-Service-forwarded access token are a distinct, separate enhancement, out of scope here).

### `AzureSSOConfiguration.cs` / `AzureSSOCredentials.IsValid()`

When Easy Auth is active, `ClientId`/`ClientSecret`/`TenantId`/`Domain`/`Instance` are never used, but `AzureSSOCredentials.IsValid()` currently requires all of them non-empty — this would make the health check permanently report "invalid" for a correctly-configured Easy Auth deployment that only sets `CallbackPath`. Relax validation: when `EasyAuthDetection.IsEnabled`, only require `CallbackPath` to be non-empty.

### No changes needed

- `MicrosoftAccountBackOfficeExternalLoginProviderOptions.cs` — already claims-shape-agnostic.
- `AzureSsoManifestReader.cs` — already driven purely by `profile.Name`/`DisplayName`/`Icon`/`ButtonStyle`, not by the underlying scheme type.
- `Settings/AzureSSOSettings.cs` — existing shape covers everything needed.

### Docs

Add a short "Azure App Service Easy Auth" section to `README.md` under Advanced usage: how it's auto-detected (no config needed beyond the normal `CallbackPath`), that it only supports a single enabled profile, and that `ClientId`/`ClientSecret`/`TenantId`/`Domain`/`Instance` aren't required in that mode.

## Verification

- Multi-target build across all TFMs: `dotnet build src/Umbraco.Community.AzureSSO/Umbraco.Community.AzureSSO.csproj` (net6.0/net7.0/net8.0/net9.0/net10.0) to confirm the `#if NET8_0_OR_GREATER` handler constructor split compiles cleanly on every target and `TreatWarningsAsErrors` stays clean.
- No test project exists in this repo currently — verification is build + manual. To smoke-test the Easy Auth path without deploying to real App Service: temporarily set `WEBSITE_AUTH_ENABLED=True` and `WEBSITE_AUTH_DEFAULT_PROVIDER=AzureActiveDirectory` as local environment variables, and inject a fake `X-MS-TOKEN-AAD-ID-TOKEN` + `X-MS-CLIENT-PRINCIPAL-IDP` header via a trivial local test middleware (or a browser extension / curl) on a locally-running Umbraco 13 or 15 site using this package, then hit the backoffice login button and confirm it redirects, "completes", and creates/links the Umbraco user with correct group mapping — this manual check should be run for at least one OLD_BACKOFFICE (net8, Umbraco 13) and one NEW_BACKOFFICE (net9, Umbraco 15) target to confirm both `BackOfficeAuthenticationBuilder` variants behave identically as verified in source.
