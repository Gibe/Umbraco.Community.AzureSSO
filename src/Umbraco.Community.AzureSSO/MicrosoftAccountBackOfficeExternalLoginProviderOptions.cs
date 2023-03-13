using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
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

		public void Configure(string name, BackOfficeExternalLoginProviderOptions options)
		{
			if (name != "Umbraco." + SchemeName)
			{
				return;
			}

			Configure(options);
		}

		public void Configure(BackOfficeExternalLoginProviderOptions options)
		{
			options.ButtonStyle = _settings.ButtonStyle;
			options.Icon = _settings.Icon;
			options.AutoLinkOptions = new ExternalSignInAutoLinkOptions(
					// must be true for auto-linking to be enabled
					autoLinkExternalAccount: true,

					// Optionally specify default user group, else
					// assign in the OnAutoLinking callback
					// (default is editor)
					defaultUserGroups: new string[] { },

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
				OnAutoLinking = SetGroups,
				OnExternalLogin = (user, loginInfo) =>
				{
					SetGroups(user, loginInfo);

					return true; //returns a boolean indicating if sign in should continue or not.
				}
			};

			// Optionally you can disable the ability for users
			// to login with a username/password. If this is set
			// to true, it will disable username/password login
			// even if there are other external login providers installed.
			options.DenyLocalLogin = _settings.DenyLocalLogin;

			// Optionally choose to automatically redirect to the
			// external login provider so the user doesn't have
			// to click the login button. This is
			options.AutoRedirectLoginToExternalProvider = false;
		}

		private void SetGroups(BackOfficeIdentityUser user, ExternalLoginInfo loginInfo)
		{
			user.Roles.Clear();

			var groups = loginInfo.Principal.Claims.Where(c => _settings.GroupLookup.ContainsKey(c.Value));
			foreach (var group in groups)
			{
				user.AddRole(_settings.GroupLookup[group.Value]);
			}

			if (loginInfo.Principal?.Identity?.Name != null)
			{
				user.Name = DisplayName(loginInfo.Principal, defaultValue: loginInfo.Principal.Identity.Name);
				user.UserName = loginInfo.Principal.Identity.Name;
			}
			user.IsApproved = true;
		}

		private string DisplayName(ClaimsPrincipal claimsPrincipal, string defaultValue)
		{
			var displayName = claimsPrincipal.GetDisplayName();

			return !string.IsNullOrWhiteSpace(displayName) ? displayName: defaultValue;
		}
	}
}
