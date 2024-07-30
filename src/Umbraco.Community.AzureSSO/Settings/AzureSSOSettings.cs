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

	public class AzureSsoProfileSettings
	{
		private readonly AzureSSOConfiguration _configuration;

		public AzureSsoProfileSettings(AzureSSOConfiguration configuration)
		{
			_configuration = configuration;
		}

		public string Name => $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}{_configuration.Name}" ?? MicrosoftAccountBackOfficeExternalLoginProviderOptions.SchemeName;
		public string? DisplayName => _configuration.DisplayName;
		public string ButtonStyle => _configuration.ButtonStyle ?? "btn-microsoft";
		public string Icon => _configuration.Icon ?? "fa fa-lock";
		public Dictionary<string, string> GroupLookup => _configuration.GroupBindings;
		public bool SetGroupsOnLogin => _configuration.SetGroupsOnLogin ?? true;
		public string[] DefaultGroups => _configuration.DefaultGroups ?? System.Array.Empty<string>();
		public bool DenyLocalLogin => _configuration.DenyLocalLogin ?? false;
		public TokenCacheType TokenCacheType => _configuration.TokenCacheType;
		public bool AutoRedirectLoginToExternalProvider => _configuration.AutoRedirectLoginToExternalProvider ?? false;
		public AzureSsoCredentialSettings Credentials => new AzureSsoCredentialSettings(_configuration.Credentials);
	}

	public class AzureSsoCredentialSettings
	{
		private readonly AzureSSOCredentials _credentials;

		public AzureSsoCredentialSettings(AzureSSOCredentials credentials)
		{
			_credentials = credentials;
		}

		public string Instance => _credentials.Instance;
		public string Domain => _credentials.Domain;
		public string TenantId => _credentials.TenantId;
		public string ClientId => _credentials.ClientId;
		public string ClientSecret => _credentials.ClientSecret;
		public string CallbackPath => _credentials.CallbackPath;
		public string SignedOutCallbackPath => _credentials.SignedOutCallbackPath;
	}
}
