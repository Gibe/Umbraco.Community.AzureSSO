using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.AzureSSO.Settings;
using Umbraco.Extensions;
using Microsoft.Extensions.Logging;

#if NEW_BACKOFFICE
using Umbraco.Cms.Api.Management.Security;
#endif
#if OLD_BACKOFFICE
using Umbraco.Cms.Web.BackOffice.Security;
#endif


namespace Umbraco.Community.AzureSSO
{
	public class MicrosoftAccountBackOfficeExternalLoginProviderOptions(AzureSsoSettings settings,
		ILogger<MicrosoftAccountBackOfficeExternalLoginProviderOptions> logger,
		IServiceProvider serviceProvider)
		: IConfigureNamedOptions<BackOfficeExternalLoginProviderOptions>
	{
		public const string SchemeName = "MicrosoftAccount";

		public void Configure(string? name, BackOfficeExternalLoginProviderOptions options)
		{

			var profile = settings.Profiles
				.FirstOrDefault(x => x.Name == name);
			if (profile == null)
			{
				return;
			}

			Configure(options, profile);
		}

		public void Configure(BackOfficeExternalLoginProviderOptions options, AzureSsoProfileSettings profileSettings)
		{
#if OLD_BACKOFFICE
#pragma warning disable CS0618 // Type or member is obsolete
			options.ButtonStyle = profileSettings.ButtonStyle;
			options.Icon = profileSettings.Icon;
#pragma warning restore CS0618 // Type or member is obsolete
#endif
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
						SetGroups(autoLoginUser, loginInfo, profileSettings);
						SetName(autoLoginUser, loginInfo);
					}
				},
				OnExternalLogin = (user, loginInfo) =>
				{
					var existingUsername = user.UserName;

					if (profileSettings.SetGroupsOnLogin)
					{
						SetGroups(user, loginInfo, profileSettings);
					}
					SetName(user, loginInfo);

					if (profileSettings.SetProfileImageOnLogin)
					{
						SetProfileImage(existingUsername, loginInfo);
					}

					if (user.Roles.Any())
					{
						return true;
					}

					return false; // Stop login if the user has no roles assigned
				}
			};

			// Optionally you can disable the ability for users
			// to log in with a username/password. If this is set
			// to true, it will disable username/password login
			// even if there are other external login providers installed.
			options.DenyLocalLogin = profileSettings.DenyLocalLogin;

#if OLD_BACKOFFICE
			// Optionally choose to automatically redirect to the
			// external login provider so the user doesn't have
			// to click the login button.
			options.AutoRedirectLoginToExternalProvider = profileSettings.AutoRedirectLoginToExternalProvider;
#endif
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

			if (settings.LogUnmappedRolesAsWarning)
			{
				var unmappedGroups = loginInfo.Principal.Claims.Where(c => !settings.GroupLookup.ContainsKey(c.Value)).Select(c => c.Value).ToArray();
				if (unmappedGroups.Any())
				{
					logger.LogWarning("The following groups were not mapped to Umbraco roles: {Groups}", string.Join(", ", unmappedGroups));
				}
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

		private void SetProfileImage(string? username, ExternalLoginInfo loginInfo)
		{
			if (string.IsNullOrEmpty(username))
			{
				return;
			}

			try
			{
				var accessToken = loginInfo.AuthenticationTokens?
					.FirstOrDefault(t => t.Name == "access_token")?.Value;

				if (string.IsNullOrEmpty(accessToken))
				{
					return;
				}

				var userService = serviceProvider.GetRequiredService<IUserService>();

				if (userService.GetByUsername(username) is not { } umbracoUser)
				{
					return;
				}

				var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
				using var httpClient = httpClientFactory.CreateClient();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

				using var response = httpClient
					.GetAsync("https://graph.microsoft.com/v1.0/me/photo/$value")
					.GetAwaiter()
					.GetResult();

				if (!response.IsSuccessStatusCode)
				{
					if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
					{
						logger.LogWarning(
							"Failed to fetch the Entra ID profile picture for user {Username}. Microsoft Graph returned {StatusCode}",
							username, response.StatusCode);
					}

					return;
				}

				var extension = response.Content.Headers.ContentType?.MediaType switch
				{
					"image/png" => "png",
					"image/gif" => "gif",
					"image/bmp" => "bmp",
					_ => "jpg"
				};

				var avatarPath = $"UserAvatars/{$"{umbracoUser.Key}profile.{extension}".GenerateHash<SHA256>()}.{extension}";

				var mediaFileManager = serviceProvider.GetRequiredService<MediaFileManager>();
				using (var photoStream = response.Content.ReadAsStream())
				{
					mediaFileManager.FileSystem.AddFile(avatarPath, photoStream, true);
				}

				umbracoUser.Avatar = avatarPath;
				userService.Save(umbracoUser);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to fetch and set the Entra ID profile picture as the avatar for user {Username}", username);
			}
		}

		public void Configure(BackOfficeExternalLoginProviderOptions options)
		{
			throw new NotImplementedException(
				"Use Configure(BackOfficeExternalLoginProviderOptions, AzureSsoProfileSettings) instead");
		}
	}
}
