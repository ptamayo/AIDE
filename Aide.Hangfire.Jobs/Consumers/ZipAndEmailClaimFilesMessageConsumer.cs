using Hangfire;
using MassTransit;
using System.Threading.Tasks;
using Aide.Hangfire.Common.Messages;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
	public class ZipAndEmailClaimFilesMessageConsumer : IConsumer<ZipAndEmailClaimFilesMessage>
	{
		public async Task Consume(ConsumeContext<ZipAndEmailClaimFilesMessage> context)
		{
			BackgroundJob.Enqueue<ClaimProbatoryDocumentsJob>(x => x.ZipClaimProbatoryDocumentsForStoreOnly(context.Message));
		}
	}
#pragma warning restore 1998
}
