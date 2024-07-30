using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.BackOffice.Security;
using Umbraco.Community.AzureSSO.Settings;

namespace Umbraco.Community.AzureSSO
{
	public class MicrosoftAccountBackOfficeExternalLoginProviderOptions : IConfigureNamedOptions<BackOfficeExternalLoginProviderOptions>
	{
		public const string SchemeName = "MicrosoftAccount";

		private readonly AzureSsoSettings _settings;

		public MicrosoftAccountBackOfficeExternalLoginProviderOptions(AzureSsoSettings settings)
		{
			_settings = settings;
		}

		public void Configure(string? name, BackOfficeExternalLoginProviderOptions options)
		{
			var profile = _settings.Profiles
				.FirstOrDefault(x => x.Name == name);
			if (profile == null)
			{
				return;
			}

			Configure(options, profile);
		}

		public void Configure(BackOfficeExternalLoginProviderOptions options, AzureSsoProfileSettings settings)
		{
			options.ButtonStyle = settings.ButtonStyle;
			options.Icon = settings.Icon;
			options.AutoLinkOptions = new ExternalSignInAutoLinkOptions(
					// must be true for auto-linking to be enabled
					autoLinkExternalAccount: true,

					// Optionally specify default user group, else
					// assign in the OnAutoLinking callback
					// (default is editor)
					defaultUserGroups: System.Array.Empty<string>(),

					// Optionally specify the default culture to create
					// the user as. If null it will use the default
					// culture defined in the web.config, or it can
					// be dynamically assigned in the OnAutoLinking
					// callback.
					defaultCulture: null,

					// Optionally you can disable the ability to link/unlink
					// manually from within the back office. Set this to false
					// if you don't want the user to unlink from this external
					// provider.
					allowManualLinking: false
			)
			{
				// Optional callback
				OnAutoLinking = (autoLoginUser, loginInfo) =>
				{
					if (!autoLoginUser.IsApproved)
					{
						SetGroups(autoLoginUser, loginInfo, settings);
						SetName(autoLoginUser, loginInfo);
					}
				},
				OnExternalLogin = (user, loginInfo) =>
				{
					if (settings.SetGroupsOnLogin)
					{
						SetGroups(user, loginInfo, settings);
					}
					SetName(user, loginInfo);

					return true; //returns a boolean indicating if sign in should continue or not.
				}
			};

			// Optionally you can disable the ability for users
			// to login with a username/password. If this is set
			// to true, it will disable username/password login
			// even if there are other external login providers installed.
			options.DenyLocalLogin = settings.DenyLocalLogin;

			// Optionally choose to automatically redirect to the
			// external login provider so the user doesn't have
			// to click the login button.
			options.AutoRedirectLoginToExternalProvider = settings.AutoRedirectLoginToExternalProvider;
		}

		private void SetGroups(BackOfficeIdentityUser user, ExternalLoginInfo loginInfo, AzureSsoProfileSettings settings)
		{
			user.Roles.Clear();

			var groups = loginInfo.Principal.Claims.Where(c => settings.GroupLookup.ContainsKey(c.Value));
			foreach (var group in groups)
			{
				var umbracoGroups = settings.GroupLookup[group.Value].Split(',');
				foreach (var umbracoGroupAlias in umbracoGroups)
				{
					user.AddRole(umbracoGroupAlias);
				}
			}

			foreach (var group in settings.DefaultGroups)
			{
				user.AddRole(group);
			}
		}

		private void SetName(BackOfficeIdentityUser user, ExternalLoginInfo loginInfo)
		{
			if (loginInfo.Principal?.Identity?.Name != null)
			{
				user.Name = DisplayName(loginInfo.Principal, defaultValue: loginInfo.Principal.Identity.Name);
				user.UserName = loginInfo.Principal.Identity.Name;
			}
			user.IsApproved = true;
		}

		private string DisplayName(ClaimsPrincipal claimsPrincipal, string defaultValue)
		{
			var displayName = claimsPrincipal.FindFirstValue("name");

			return !string.IsNullOrWhiteSpace(displayName) ? displayName : defaultValue;
		}

		public void Configure(BackOfficeExternalLoginProviderOptions options)
		{
			// TODO : What do we do here
		}
	}
}
