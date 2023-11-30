using Aide.Hangfire.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs
{
    public class OrderManagementJob
    {
        private readonly ILogger<OrderManagementJob> _logger;
        private readonly IClaimService _claimService;

        public OrderManagementJob(ILogger<OrderManagementJob> logger, IClaimService claimService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _claimService = claimService ?? throw new ArgumentNullException(nameof(claimService));
        }

        public async Task RemoveStaledOrders(double thresholdInHours)
        {
            try
            {
                var result = await _claimService.RemoveStaledOrders(thresholdInHours).ConfigureAwait(false);
                _logger.LogInformation(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred when deleting the stale orders that are older than {thresholdInHours} hours.");
                throw;
            }
        }
    }
}
