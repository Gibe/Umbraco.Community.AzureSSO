using System.Collections.Generic;

namespace Umbraco.Community.AzureSSO.Settings
{
	public class AzureSsoSettings
	{
		private readonly AzureSSOConfiguration _configuration;

		public AzureSsoSettings(AzureSSOConfiguration configuration)
		{
			_configuration = configuration;
		}

		public string ButtonStyle => _configuration.ButtonStyle ?? "btn-microsoft";
		public string Icon => _configuration.Icon ?? "fa fa-lock";
		public Dictionary<string, string> GroupLookup => _configuration.GroupBindings;
		public bool SetGroupsOnLogin => _configuration.SetGroupsOnLogin ?? true;
		public string[] DefaultGroups => _configuration.DefaultGroups ?? System.Array.Empty<string>();
		public bool DenyLocalLogin => _configuration.DenyLocalLogin ?? false;
		public bool AutoRedirectLoginToExternalProvider => _configuration.AutoRedirectLoginToExternalProvider ?? false;
	}
}
