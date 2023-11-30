using Hangfire;
using Aide.Hangfire.Common.Messages;
using MassTransit;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
	public class UpdateClaimMessageConsumer : IConsumer<UpdateClaimMessage>
	{
		public async Task Consume(ConsumeContext<UpdateClaimMessage> context)
		{
			BackgroundJob.Enqueue<ClaimManagementJob>(x => x.UpdateClaim(context.Message));
		}
	}
#pragma warning restore 1998
}
