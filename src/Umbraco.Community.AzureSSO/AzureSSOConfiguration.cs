using System.Collections.Generic;

namespace Umbraco.Community.AzureSSO
{
	public class AzureSSOConfiguration
	{
		public const string AzureSsoSectionName = "AzureSSO";

		public string? Name { get; set; }

		public string? DisplayName { get; set; }

		public string? ButtonStyle { get; set; }

		public string? Icon { get; set; }

		public Dictionary<string, string> GroupBindings { get; set; } = new();

		public bool? SetGroupsOnLogin { get; set; }

		public string[]? DefaultGroups { get; set; }

		public bool? DenyLocalLogin { get; set; }

		public TokenCacheType TokenCacheType { get; set; } = TokenCacheType.InMemory;

		public bool? AutoRedirectLoginToExternalProvider { get; set; }

		public AzureSSOCredentials? Credentials { get; set; }

		public AzureSSOConfiguration[]? Profiles { get; set; }
	}

	public class AzureSSOCredentials
	{
		public string Instance { get; set; }
		public string Domain { get; set; }
		public string TenantId { get; set; }
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
		public string CallbackPath { get; set; }
		public string SignedOutCallbackPath { get; set; }
	}
}
