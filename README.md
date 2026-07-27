# Umbraco.Community.AzureSSO

Add Azure Entra ID SSO to Umbraco v10+ sites. This will allow you to automatically create Umbraco user accounts for users in your Microsoft Entra ID tenant. This will then associate the Umbraco users with groups based on their group, and the configuration below.

First you, or an Entra ID administrator will need to create an App Registration in the Azure Portal which will be used to authenticate the site against Azure AD. Follow [these instructions to setup the new App Registration](EntraIDSetup.md)

To install:

`dotnet add package Umbraco.Community.AzureSSO`

### Setup

Select the instructions for your Umbraco version

[v10-12](README-uptov12.md)
[v13](README-v13.md)
[v15+](README-v15plus.md)

## Advanced usage

### Manually composing

If you'd like to disable the composer and configure it yourself you can add DisableComposer to the settings and set it's value to true

i.e.
```
"AzureSSO": {
	/// All the other configuration
	"DisableComposer": true
}
```

In which case you'll need to add AddMicrosoftAccountAuthentication() to your ConfigureServices function

### Debugging

LogUnmappedRolesAsWarning
When SetGroupsOnLogin is set to true, if LogUnmappedRolesAsWarning is also set to true this will log as warning for unmapped Entra ID groups, where the Entra ID name has a slash \ in it. By design it does not log everything to prevent logging of email addresses and so on.

### Azure App Service Easy Auth

If your site is hosted on Azure App Service with [built-in authentication (Easy Auth)](https://learn.microsoft.com/en-us/azure/app-service/overview-authentication-authorization) enabled for the Microsoft/Entra ID provider, this package automatically detects it (via the `WEBSITE_AUTH_ENABLED`/`WEBSITE_AUTH_DEFAULT_PROVIDER` environment variables App Service sets) and signs backoffice users in using the App Service session instead of performing its own OpenID Connect handshake.

In this mode:

- `Credentials.ClientId`, `ClientSecret`, `TenantId`, `Domain` and `Instance` aren't used - App Service already owns the AAD app registration and token exchange. Only `CallbackPath` and `SignedOutCallbackPath` are required (they're used as internal dispatch routes, not as OIDC redirect URIs).
- `GroupBindings`, `DefaultGroups`, `SetGroupsOnLogin`, `DenyLocalLogin` and the other backoffice settings all work exactly as they do with the normal OIDC flow.
- Only a single `Enabled` profile is supported - Easy Auth is one site-wide identity, so it can't be split across multiple profiles/tenants. The site will fail to start with a clear error if more than one profile is enabled while Easy Auth is active.
- Logging out of the Umbraco backoffice also ends the App Service Easy Auth session, so users aren't silently signed back in on their next visit.
