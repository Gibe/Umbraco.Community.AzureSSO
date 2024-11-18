using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core;

namespace Umbraco.Community.AzureSSO.Settings
{
	public class AzureSsoSettings
	{
		public AzureSsoSettings(AzureSSOConfiguration configuration)
		{
			if (configuration.Profiles == null)
			{
				Profiles = new[] { new AzureSsoProfileSettings(configuration) };
				return;
			}

			Profiles = configuration.Profiles.Select(x => new AzureSsoProfileSettings(x)).ToArray();
		}
		public AzureSsoProfileSettings[] Profiles { get; }
	}

	public class AzureSsoProfileSettings(AzureSSOConfiguration configuration)
	{
		public string Name => $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}" + (string.IsNullOrEmpty(configuration.Name) ? MicrosoftAccountBackOfficeExternalLoginProviderOptions.SchemeName : configuration.Name);
		public string? DisplayName => configuration.DisplayName;
		public string ButtonStyle => configuration.ButtonStyle ?? "btn-microsoft";
		public string Icon => configuration.Icon ?? "fa fa-lock";
		public Dictionary<string, string> GroupLookup => configuration.GroupBindings;
		public bool SetGroupsOnLogin => configuration.SetGroupsOnLogin ?? true;
		public bool LogUnmappedRolesAsWarning => configuration.LogUnmappedRolesAsWarning ?? false;
		public string[] DefaultGroups => configuration.DefaultGroups ?? System.Array.Empty<string>();
		public bool DenyLocalLogin => configuration.DenyLocalLogin ?? false;
		public TokenCacheType TokenCacheType => configuration.TokenCacheType;
		public bool AutoRedirectLoginToExternalProvider => configuration.AutoRedirectLoginToExternalProvider ?? false;
		public AzureSsoCredentialSettings Credentials => new AzureSsoCredentialSettings(configuration.Credentials);
	}

	public class AzureSsoCredentialSettings(AzureSSOCredentials credentials)
	{
		public string Instance => credentials.Instance;
		public string Domain => credentials.Domain;
		public string TenantId => credentials.TenantId;
		public string ClientId => credentials.ClientId;
		public string ClientSecret => credentials.ClientSecret;
		public string CallbackPath => credentials.CallbackPath;
		public string SignedOutCallbackPath => credentials.SignedOutCallbackPath;
	}
}
