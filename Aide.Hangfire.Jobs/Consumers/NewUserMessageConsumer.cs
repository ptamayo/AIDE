using Hangfire;
using Aide.Hangfire.Common.Messages;
using MassTransit;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
	public class NewUserMessageConsumer : IConsumer<NewUserMessage>
	{
		public async Task Consume(ConsumeContext<NewUserMessage> context)
		{
			BackgroundJob.Enqueue<UserManagementJob>(x => x.SendWelcomeEmailAsync(context.Message));
		}
	}
#pragma warning restore 1998
}
