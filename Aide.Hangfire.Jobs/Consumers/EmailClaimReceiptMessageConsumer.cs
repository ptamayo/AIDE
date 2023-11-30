using Hangfire;
using Aide.Hangfire.Common.Messages;
using MassTransit;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
	public class EmailClaimReceiptMessageConsumer : IConsumer<EmailClaimReceiptMessage>
	{
		public async Task Consume(ConsumeContext<EmailClaimReceiptMessage> context)
		{
			BackgroundJob.Enqueue<ClaimReceiptJob>(x => x.SendEmailAsync(context.Message));
		}
	}
#pragma warning restore 1998
}
