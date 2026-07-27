using Microsoft.Identity.Web;

namespace Umbraco.Community.AzureSSO.EasyAuth
{
	public static class EasyAuthDetection
	{
		public static bool IsEnabled => AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled;
	}
}
