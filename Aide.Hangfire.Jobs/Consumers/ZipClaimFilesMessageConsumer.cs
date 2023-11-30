using Hangfire;
using MassTransit;
using System.Threading.Tasks;
using Aide.Hangfire.Common.Messages;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
	public class ZipClaimFilesMessageConsumer : IConsumer<ZipClaimFilesMessage>
	{
		public async Task Consume(ConsumeContext<ZipClaimFilesMessage> context)
		{
			BackgroundJob.Enqueue<ClaimProbatoryDocumentsJob>(x => x.ZipClaimProbatoryDocuments(context.Message));
		}
	}
#pragma warning restore 1998
}
