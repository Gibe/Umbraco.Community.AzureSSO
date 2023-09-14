using System.Collections.Generic;

namespace Umbraco.Community.AzureSSO
{
	public class AzureSSOConfiguration
	{
		public AzureSSOConfiguration()
		{
			GroupBindings = new Dictionary<string, string>();
		}

		public string? DisplayName { get; set; }

		public string? ButtonStyle { get; set; }

		public string? Icon { get; set; }

		public Dictionary<string, string> GroupBindings { get; set; }

		public bool? SetGroupsOnLogin { get; set; }

		public string[]? DefaultGroups { get; set; }

		public bool? DenyLocalLogin { get; set; }

		public TokenCacheType TokenCacheType { get; set; } = TokenCacheType.InMemory;
	}
}
