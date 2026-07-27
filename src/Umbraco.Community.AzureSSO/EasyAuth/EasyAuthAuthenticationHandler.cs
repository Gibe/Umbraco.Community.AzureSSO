using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Identity.Web;

namespace Umbraco.Community.AzureSSO.EasyAuth
{
	/// <summary>
	/// Drives the Umbraco backoffice external-login flow (challenge/callback/sign-out) against Azure App Service's
	/// built-in Easy Auth session, instead of performing an OpenID Connect handshake ourselves.
	/// </summary>
	/// <remarks>
	/// Microsoft.Identity.Web's own AppServicesAuthenticationInformation.GetUser(...) does exactly this, but it's
	/// internal to that assembly, so the token/claims handling below is a small reimplementation of it against the
	/// documented, stable Azure App Service authentication headers.
	/// </remarks>
	public class EasyAuthAuthenticationHandler : RemoteAuthenticationHandler<EasyAuthAuthenticationOptions>, IAuthenticationSignOutHandler
	{
		private const string ReturnUrlParameter = "returnUrl";
		private const string IdTokenHeader = "X-MS-TOKEN-AAD-ID-TOKEN";
		private const string ClientPrincipalIdpHeader = "X-MS-CLIENT-PRINCIPAL-IDP";

		public EasyAuthAuthenticationHandler(
			IOptionsMonitor<EasyAuthAuthenticationOptions> options,
			ILoggerFactory logger,
#if NET8_0_OR_GREATER
			UrlEncoder encoder)
			: base(options, logger, encoder)
#else
			UrlEncoder encoder,
			ISystemClock clock)
			: base(options, logger, encoder, clock)
#endif
		{
		}

		protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
		{
			if (!EasyAuthDetection.IsEnabled)
			{
				return Task.FromResult(HandleRequestResult.Fail("Azure App Service Easy Auth is not enabled on this host."));
			}

			var principal = GetUserFromHeaders();
			if (principal == null)
			{
				return Task.FromResult(HandleRequestResult.Fail("Azure App Service did not attach an authenticated user to this request."));
			}

			EnsureNameIdentifierClaim(principal);

			string? returnUrl = Request.Query[ReturnUrlParameter];
			var properties = new AuthenticationProperties
			{
				RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl
			};

			var ticket = new AuthenticationTicket(principal, properties, Scheme.Name);
			return Task.FromResult(HandleRequestResult.Success(ticket));
		}

		protected override Task HandleChallengeAsync(AuthenticationProperties properties)
		{
			var returnUrl = properties.RedirectUri ?? "/umbraco";
			var callbackPath = Options.CallbackPath.HasValue ? Options.CallbackPath.Value! : "/";
			var callbackUrl = BuildAbsoluteUrl(QueryHelpers.AddQueryString(callbackPath, ReturnUrlParameter, returnUrl));

			Response.Redirect(QueryHelpers.AddQueryString("/.auth/login/aad", "post_login_redirect_uri", callbackUrl));
			return Task.CompletedTask;
		}

		public Task SignOutAsync(AuthenticationProperties? properties)
		{
			var logoutUrl = AppServicesAuthenticationInformation.LogoutUrl ?? "/.auth/logout";
			var returnUrl = properties?.RedirectUri;
			if (string.IsNullOrEmpty(returnUrl))
			{
				returnUrl = Options.SignedOutCallbackPath.HasValue ? Options.SignedOutCallbackPath.Value! : "/umbraco";
			}

			Response.Redirect(QueryHelpers.AddQueryString(logoutUrl, "post_logout_redirect_uri", BuildAbsoluteUrl(returnUrl)));
			return Task.CompletedTask;
		}

		private string BuildAbsoluteUrl(string path)
		{
			return Uri.IsWellFormedUriString(path, UriKind.Absolute) ? path : $"{Request.Scheme}://{Request.Host}{path}";
		}

		private ClaimsPrincipal? GetUserFromHeaders()
		{
			string? idToken = Context.Request.Headers[IdTokenHeader];
			string? idp = Context.Request.Headers[ClientPrincipalIdpHeader];
			if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(idp))
			{
				return null;
			}

			var jsonWebToken = new JsonWebToken(idToken);
			var identity = new ClaimsIdentity(jsonWebToken.Claims, idp, ClaimConstants.PreferredUserName, ClaimConstants.Roles);
			return new ClaimsPrincipal(identity);
		}

		private static void EnsureNameIdentifierClaim(ClaimsPrincipal principal)
		{
			if (principal.Identity is not ClaimsIdentity identity || identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
			{
				return;
			}

			var providerKey = principal.FindFirst(ClaimConstants.Oid)?.Value
				?? principal.FindFirst(ClaimConstants.ObjectId)?.Value
				?? principal.FindFirst(ClaimConstants.Sub)?.Value;

			if (!string.IsNullOrEmpty(providerKey))
			{
				identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, providerKey));
			}
		}
	}
}
