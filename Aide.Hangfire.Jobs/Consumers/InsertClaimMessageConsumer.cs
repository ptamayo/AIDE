using Hangfire;
using Aide.Hangfire.Common.Messages;
using MassTransit;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
	public class InsertClaimMessageConsumer : IConsumer<InsertClaimMessage>
	{
		public async Task Consume(ConsumeContext<InsertClaimMessage> context)
		{
			BackgroundJob.Enqueue<ClaimManagementJob>(x => x.InsertClaim(context.Message));
		}
	}
#pragma warning restore 1998
}
