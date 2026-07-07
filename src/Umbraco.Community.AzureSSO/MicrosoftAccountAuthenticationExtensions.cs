using System;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.AzureSSO.Settings;
using Umbraco.Extensions;
using System.Linq;


#if NEW_BACKOFFICE
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Infrastructure.Manifest;
#endif
#if OLD_BACKOFFICE
using Umbraco.Cms.Web.BackOffice.Security;
#endif

namespace Umbraco.Community.AzureSSO
{
	public static class MicrosoftAccountAuthenticationExtensions
	{
		internal static IUmbracoBuilder AddMicrosoftAccountAuthenticationInternal(this IUmbracoBuilder builder)
		{
			var azureSsoConfiguration = new AzureSSOConfiguration();
			builder.Config.Bind(AzureSSOConfiguration.AzureSsoSectionName, azureSsoConfiguration);
			builder.Services.AddSingleton<AzureSSOConfiguration>(conf => azureSsoConfiguration);


			var settings = new AzureSsoSettings(azureSsoConfiguration);
			builder.Services.AddSingleton<AzureSsoSettings>(conf => settings);

			if (settings.Profiles.All(x => !x.Enabled))
			{
				// if no profiles are enabled, we don't need to do anything
				return builder;
			}


			builder.Services.ConfigureOptions<MicrosoftAccountBackOfficeExternalLoginProviderOptions>();

#if NEW_BACKOFFICE
			builder.Services.AddSingleton<IPackageManifestReader, AzureSsoManifestReader>();
#endif

			var initialScopes = Array.Empty<string>();
			builder.AddBackOfficeExternalLogins(logins =>
				{
					foreach (var profile in settings.Profiles)
					{
						if (profile.Enabled)
						{
							logins.AddBackOfficeLogin(
								backOfficeAuthenticationBuilder =>
								{
									backOfficeAuthenticationBuilder.AddMicrosoftIdentityWebApp(options =>
											{
												CopyCredentials(options, profile.Credentials);
												options.SignInScheme = SchemeForBackOffice(profile.Name, backOfficeAuthenticationBuilder);
												options.Events = new OpenIdConnectEvents();

											},
											displayName: profile.DisplayName ?? "Microsoft Entra ID",
											cookieScheme: $"{profile.Name}Cookies",
											openIdConnectScheme: SchemeForBackOffice(profile.Name, backOfficeAuthenticationBuilder) ??
																					 String.Empty)
										.EnableTokenAcquisitionToCallDownstreamApi(
											options => CopyCredentials(options, profile.Credentials),
											initialScopes)
										.AddTokenCaches(profile.TokenCacheType);
								});
						}
					}
				}

			);

			return builder;
		}

		public static IUmbracoBuilder AddMicrosoftAccountAuthentication(this IUmbracoBuilder builder)
		{
			var disableComposer = builder.Config.GetSection(AzureSSOConfiguration.AzureSsoSectionName).GetValue<bool>("DisableComposer");

			// if composer is enabled don't add
			return disableComposer ? builder.AddMicrosoftAccountAuthenticationInternal() : builder;
		}

		private static string? SchemeForBackOffice(string name, BackOfficeAuthenticationBuilder backOfficeAuthenticationBuilder)
		{
#if OLD_BACKOFFICE
			return backOfficeAuthenticationBuilder.SchemeForBackOffice(name);
#elif NEW_BACKOFFICE
			return BackOfficeAuthenticationBuilder.SchemeForBackOffice(name);
#endif

		}

		private static void CopyCredentials(MicrosoftIdentityOptions options, AzureSsoCredentialSettings settings)
		{
			options.Instance = settings.Instance;
			options.Domain = settings.Domain;
			options.TenantId = settings.TenantId;
			options.ClientId = settings.ClientId;
			options.SignedOutCallbackPath = settings.SignedOutCallbackPath;
			options.CallbackPath = settings.CallbackPath;

			switch (settings.CredentialType)
			{
				case CredentialType.WorkloadIdentity:
					var tokenFilePath = Environment.GetEnvironmentVariable("AZURE_FEDERATED_TOKEN_FILE");
					if (string.IsNullOrEmpty(tokenFilePath))
					{
						throw new InvalidOperationException(
							"CredentialType is WorkloadIdentity but the AZURE_FEDERATED_TOKEN_FILE environment variable is not set. " +
							"Ensure the Azure Workload Identity webhook is configured and the pod has the azure.workload.identity/use: \"true\" label.");
					}

					options.ClientCredentials = new[]
					{
						new CredentialDescription
						{
							SourceType = CredentialSource.SignedAssertionFilePath,
							SignedAssertionFileDiskPath = tokenFilePath,
						}
					};
					break;
				case CredentialType.ManagedIdentity:
					options.ClientCredentials = new[]
					{
						new CredentialDescription
						{
							SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
							// ManagedIdentityClientId is the user-assigned managed identity client ID; leave empty for system-assigned.
							ManagedIdentityClientId = settings.ManagedIdentityClientId,
						}
					};
					break;
				case CredentialType.Secret:
				default:
					options.ClientSecret = settings.ClientSecret;
					break;
			}
		}

		private static void CopyCredentials(ConfidentialClientApplicationOptions options, AzureSsoCredentialSettings settings)
		{
			options.Instance = settings.Instance;
			options.TenantId = settings.TenantId;
			options.ClientId = settings.ClientId;

			if (settings.CredentialType == CredentialType.Secret)
			{
				options.ClientSecret = settings.ClientSecret;
			}
		}

		private static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddTokenCaches(this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder, TokenCacheType tokenCacheType)
		{
			switch (tokenCacheType)
			{
				case TokenCacheType.Session:
					builder.AddSessionTokenCaches();
					break;
				case TokenCacheType.Distributed:
					builder.AddDistributedTokenCaches();
					break;
				case TokenCacheType.InMemory:
				default:
					builder.AddInMemoryTokenCaches();
					break;
			}

			return builder;
		}

	}
}


