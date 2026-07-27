using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Umbraco.Community.AzureSSO.EasyAuth
{
	public class EasyAuthAuthenticationOptions : RemoteAuthenticationOptions
	{
		/// <summary>
		/// Where to send the user after Azure App Service Easy Auth ends the site-wide session on backoffice logout.
		/// </summary>
		public PathString SignedOutCallbackPath { get; set; }
	}
}
