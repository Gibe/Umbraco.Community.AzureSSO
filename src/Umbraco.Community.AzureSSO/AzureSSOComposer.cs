using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.AzureSSO
{
	public class AzureSSOComposer : IComposer
	{
		public void Compose(IUmbracoBuilder builder)
		{
			builder.AddMicrosoftAccountAuthentication();
		}
	}
}
