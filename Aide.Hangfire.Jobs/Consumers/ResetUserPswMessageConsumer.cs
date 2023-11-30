using Hangfire;
using Aide.Hangfire.Common.Messages;
using MassTransit;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
	public class ResetUserPswMessageConsumer : IConsumer<ResetUserPswMessage>
	{
		public async Task Consume(ConsumeContext<ResetUserPswMessage> context)
		{
			BackgroundJob.Enqueue<UserManagementJob>(x => x.SendPswResetEmailAsync(context.Message));
		}
	}
#pragma warning restore 1998
}
