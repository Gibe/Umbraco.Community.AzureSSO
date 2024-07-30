using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.AzureSSO.Settings;
using Umbraco.Extensions;

namespace Umbraco.Community.AzureSSO
{
	public static class MicrosoftAccountAuthenticationExtensions
	{
		internal static IUmbracoBuilder AddMicrosoftAccountAuthentication(this IUmbracoBuilder builder)
		{
			var azureSsoConfiguration = new AzureSSOConfiguration();
			builder.Config.Bind(AzureSSOConfiguration.AzureSsoSectionName, azureSsoConfiguration);

			var settings = new AzureSsoSettings(azureSsoConfiguration);
			builder.Services.AddSingleton<AzureSsoSettings>(conf => settings);
			builder.Services.ConfigureOptions<MicrosoftAccountBackOfficeExternalLoginProviderOptions>();
			
			var initialScopes = Array.Empty<string>();
			builder.AddBackOfficeExternalLogins(logins =>
				{
					foreach (var profile in settings.Profiles)
					{
						logins.AddBackOfficeLogin(
							backOfficeAuthenticationBuilder =>
							{
								backOfficeAuthenticationBuilder.AddMicrosoftIdentityWebApp(options =>
										{
											CopyCredentials(options, profile.Credentials);
											options.SignInScheme = backOfficeAuthenticationBuilder.SchemeForBackOffice(profile.Name);
											options.Events = new OpenIdConnectEvents();
											
										},
										displayName: profile.DisplayName ?? "Azure Active Directory",
										cookieScheme: profile.Name + "Cookies",
										openIdConnectScheme: backOfficeAuthenticationBuilder.SchemeForBackOffice(profile.Name) ??
										                     String.Empty)
									.EnableTokenAcquisitionToCallDownstreamApi(
										options => CopyCredentials(options, profile.Credentials),
										initialScopes)
									.AddTokenCaches(profile.TokenCacheType);
							});
					}
				}
			
			);
			
			return builder;
		}

		private static void CopyCredentials(MicrosoftIdentityOptions options, AzureSsoCredentialSettings settings)
		{
			options.Instance = settings.Instance;
			options.Domain = settings.Domain;
			options.TenantId = settings.TenantId;
			options.ClientId = settings.ClientId;
			options.ClientSecret = settings.ClientSecret;
			options.SignedOutCallbackPath = settings.SignedOutCallbackPath;
			options.CallbackPath = settings.CallbackPath;
		}

		private static void CopyCredentials(ConfidentialClientApplicationOptions options, AzureSsoCredentialSettings settings)
		{
			options.Instance = settings.Instance;
			options.TenantId = settings.TenantId;
			options.ClientId = settings.ClientId;
			options.ClientSecret = settings.ClientSecret;
		}

		private static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddTokenCaches(this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder, TokenCacheType tokenCacheType)
		{
			switch (tokenCacheType)
			{
				case TokenCacheType.InMemory:
					builder.AddInMemoryTokenCaches();
					break;
				case TokenCacheType.Session:
					builder.AddSessionTokenCaches();
					break;
				case TokenCacheType.Distributed:
					builder.AddDistributedTokenCaches();
					break;
				default:
					builder.AddInMemoryTokenCaches();
					break;
			}

			return builder;
		}

	}
}
