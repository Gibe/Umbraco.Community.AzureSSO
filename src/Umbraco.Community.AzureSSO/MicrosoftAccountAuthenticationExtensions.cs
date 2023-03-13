using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.AzureSSO.Settings;
using Umbraco.Extensions;

namespace Umbraco.Community.AzureSSO
{
	public static class MicrosoftAccountAuthenticationExtensions
	{
		public static IUmbracoBuilder AddMicrosoftAccountAuthentication(this IUmbracoBuilder builder, IConfiguration configuration)
		{
			var azureSsoConfiguration = new AzureSSOConfiguration();
			configuration.Bind("AzureSSO", azureSsoConfiguration);

			builder.Services.AddSingleton<AzureSsoSettings>(conf => new AzureSsoSettings(azureSsoConfiguration));
			builder.Services.ConfigureOptions<MicrosoftAccountBackOfficeExternalLoginProviderOptions>();
			
			var initialScopes = new string[] { };
			builder.AddBackOfficeExternalLogins(logins =>
			{
				logins.AddBackOfficeLogin(
					backOfficeAuthenticationBuilder =>
					{
						backOfficeAuthenticationBuilder.AddMicrosoftIdentityWebApp(options =>
								{
									configuration.Bind("AzureSSO:Credentials", options);
									options.SignInScheme = backOfficeAuthenticationBuilder.SchemeForBackOffice(MicrosoftAccountBackOfficeExternalLoginProviderOptions.SchemeName);
									options.Events = new OpenIdConnectEvents();
								},
								options => { configuration.Bind("AzureSSO:Credentials", options); },
								displayName: azureSsoConfiguration.DisplayName ?? "Azure Active Directory",
								openIdConnectScheme: backOfficeAuthenticationBuilder.SchemeForBackOffice(MicrosoftAccountBackOfficeExternalLoginProviderOptions.SchemeName) ?? String.Empty)
							.EnableTokenAcquisitionToCallDownstreamApi(options => configuration.Bind("AzureSSO:Credentials", options), initialScopes)
							.AddInMemoryTokenCaches();


					});
			});
			return builder;
		}
		
	}

	public class AzureSSOConfiguration
	{
		public AzureSSOConfiguration()
		{
			GroupBindings = new Dictionary<string, string>();
		}

		public string? DisplayName { get; set; }
		public string? ButtonStyle { get; set; }
		public string? Icon { get; set; }
		public Dictionary<string,string> GroupBindings { get; set; }
		public bool? DenyLocalLogin { get; set; }
 	}
}
