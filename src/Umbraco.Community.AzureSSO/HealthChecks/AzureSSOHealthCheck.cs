using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.HealthChecks;

namespace Umbraco.Community.AzureSSO.HealthChecks
{
	[HealthCheck(HealthCheckId, HealthCheckName, Description = "Checks the Azure SSO config to ensure it is valid.", Group = "Configuration")]
	public class AzureSSOHealthCheck : HealthCheck
	{
		private const string HealthCheckId = "07F7DA0A-D351-4347-92B3-9B607E1D38BB";
		private const string HealthCheckName = "Azure SSO";

		private AzureSSOConfiguration _configuration;

		public AzureSSOHealthCheck(AzureSSOConfiguration configuration)
		{
			_configuration = configuration;
		}

#if NET10_0
		public override async Task<IEnumerable<HealthCheckStatus>> GetStatusAsync()
		{
			return GetStatuses();
		}
#else
		public override async Task<IEnumerable<HealthCheckStatus>> GetStatus()
		{
			return GetStatuses();
		}
#endif

		private List<HealthCheckStatus> GetStatuses()
		{
			var statuses = new List<HealthCheckStatus>();
			if (!_configuration.IsValid())
			{
				statuses.Add(new HealthCheckStatus("Configuration Invalid.")
				{
					Description = "Check AzureSSO configuration",
					ResultType = StatusResultType.Error
				});
			}
			else
			{
				statuses.Add(new HealthCheckStatus("Configuration valid.")
				{
					ResultType = StatusResultType.Success
				});
			}
			return statuses;
		}

		public override HealthCheckStatus ExecuteAction(HealthCheckAction action) => new("How did you get here?")
		{
			ResultType = StatusResultType.Info
		};
	}
}
