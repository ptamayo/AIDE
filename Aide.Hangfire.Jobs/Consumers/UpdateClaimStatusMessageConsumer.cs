using Hangfire;
using Aide.Hangfire.Common.Messages;
using MassTransit;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
	public class UpdateClaimStatusMessageConsumer : IConsumer<UpdateClaimStatusMessage>
	{
		public async Task Consume(ConsumeContext<UpdateClaimStatusMessage> context)
		{
			BackgroundJob.Enqueue<ClaimManagementJob>(x => x.UpdateClaimStatus(context.Message));
		}
	}
#pragma warning restore 1998
}
