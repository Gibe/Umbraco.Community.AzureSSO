using Microsoft.Extensions.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.AzureSSO
{
	public class AzureSSOComposer : IComposer
	{
		public void Compose(IUmbracoBuilder builder)
		{
			var disableComposer = builder.Config.GetSection(AzureSSOConfiguration.AzureSsoSectionName).GetValue<bool>("DisableComposer");
			if (!disableComposer)
			{
				builder.AddMicrosoftAccountAuthenticationInternal();
			}
		}
	}
}
