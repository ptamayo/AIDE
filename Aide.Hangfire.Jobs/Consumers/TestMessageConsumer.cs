using Hangfire;
using Aide.Core.Cloud.Azure.ServiceBus;
using MassTransit;
using System;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
	public class TestMessageConsumer : IConsumer<TestMessage>
	{
		public async Task Consume(ConsumeContext<TestMessage> context)
		{
			BackgroundJob.Enqueue(() => Console.WriteLine($"TestMessage received: {context.Message.Content}"));
		}
	}
#pragma warning restore 1998
}
