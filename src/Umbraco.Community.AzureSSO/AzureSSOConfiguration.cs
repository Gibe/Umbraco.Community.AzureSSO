using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Tokens;

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
		public bool DisableComposer { get; set; } = false;

		public bool IsValid()
		{
			// TODO : Make this give or log specific feedback before we do anything like prevent booting if misconfigured
			//        and to make it more useful for the HealthCheck
			return (Profiles != null && Profiles.Any() && AllValuesEmpty() && AllProfilesUnique() && AllProfilesHaveName()) ||
			       (Profiles.IsNullOrEmpty() && Credentials != null && Credentials.IsValid());
		}

		public bool AllValuesEmpty()
		{
			return string.IsNullOrEmpty(Name) &&
			       string.IsNullOrEmpty(DisplayName) &&
			       string.IsNullOrEmpty(ButtonStyle) &&
			       string.IsNullOrEmpty(Icon) &&
			       !GroupBindings.Any() &&
			       SetGroupsOnLogin == null &&
			       (DefaultGroups == null || !DefaultGroups.Any()) &&
			       DenyLocalLogin == null &&
			       AutoRedirectLoginToExternalProvider == null &&
			       Credentials == null;
		}

		public bool AllProfilesHaveName()
		{
			return Profiles != null && Profiles.All(x => !string.IsNullOrEmpty(x.Name));
		}

		public bool AllProfilesUnique()
		{
			return Profiles != null &&
				     Profiles.Select(x => x.Name).Distinct().Count() == Profiles.Count() &&
			       Profiles.Select(x => x.Credentials?.CallbackPath).Distinct().Count() == Profiles.Count() &&
			       Profiles.Select(x => x.Credentials?.SignedOutCallbackPath).Distinct().Count() == Profiles.Count() &&
			       Profiles.Select(x => x.DisplayName).Distinct().Count() == Profiles.Count();
		}
	}

	public class AzureSSOCredentials
	{
		public string Instance { get; set; } = "";
		public string Domain { get; set; } = "";
		public string TenantId { get; set; } = "";
		public string ClientId { get; set; } = "";
		public string ClientSecret { get; set; } = "";
		public string CallbackPath { get; set; } = "";
		public string SignedOutCallbackPath { get; set; } = "";

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(Instance) &&
			       !string.IsNullOrEmpty(Domain) &&
			       !string.IsNullOrEmpty(TenantId) &&
			       !string.IsNullOrEmpty(ClientId) &&
			       !string.IsNullOrEmpty(ClientSecret) &&
			       !string.IsNullOrEmpty(CallbackPath) &&
			       !string.IsNullOrEmpty(SignedOutCallbackPath);
		}
	}
}
