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
		public bool DenyLocalLogin => _configuration.DenyLocalLogin ?? false;
	}
}
