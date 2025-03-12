#if NEW_BACKOFFICE
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Community.AzureSSO.Settings;

namespace Umbraco.Community.AzureSSO
{
	public class AzureSsoManifestReader(AzureSsoSettings azureSsoSettings) : IPackageManifestReader
	{
		public async Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
		{
			return await Task.Run(() =>
			{
				var extensions = new List<JsonObject>();

				foreach (var profile in azureSsoSettings.Profiles)
				{
					var look = "primary";
					var color = "default";

					if (profile.ButtonStyle.Contains('-') && profile.ButtonStyle != "btn-microsoft")
					{
						var buttonStyle = profile.ButtonStyle.Split('-');
						look = buttonStyle[0];
						color = buttonStyle[1];
					}
					else if (profile.ButtonStyle != "btn-microsoft")
					{
						look = profile.ButtonStyle;
					}

					var meta = new JsonObject
					{
						["label"] = profile.DisplayName,
						["defaultView"] = new JsonObject
						{
							["icon"] = profile.Icon,
							["look"] = look,
							["color"] = color
						},
						["behavior"] = new JsonObject
						{
							["autoRedirect"] = profile.AutoRedirectLoginToExternalProvider
						},
						["linking"] = new JsonObject
						{
							["allowManualLinking"] = false
						}
					};

					var extension = new JsonObject
					{
						["name"] = "Umbraco.Community.AzureSSO" + profile.Name,
						["alias"] = "Umbraco.Community.AzureSSO" + profile.Name.Replace(" ", ""),
						["type"] = "authProvider",
						["forProviderName"] = profile.Name,
						["meta"] = meta
					};

					extensions.Add(extension);
					
					var icons = new JsonObject
					{
						["type"] = "icons",
						["alias"] = "Umbraco.Community.AzureSSO.Icons",
						["name"] = "AzureSSO Icons",
						["js"] = "/App_Plugins/AzureSSO/Icons/icons.js",
					};

					extensions.Add(icons);
				}

				return new List<PackageManifest>() {
					new PackageManifest()
					{
						AllowTelemetry = true,
						Name = "Umbraco Community AzureSSO",
						Extensions = [.. extensions],
						AllowPublicAccess = true
					}
				};
			});
		}
	}
}
#endif
