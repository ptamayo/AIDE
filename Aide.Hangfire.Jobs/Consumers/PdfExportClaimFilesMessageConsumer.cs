using Hangfire;
using Aide.Hangfire.Common.Messages;
using MassTransit;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
	public class PdfExportClaimFilesMessageConsumer : IConsumer<PdfExportClaimFilesMessage>
	{
		public async Task Consume(ConsumeContext<PdfExportClaimFilesMessage> context)
		{
			BackgroundJob.Enqueue<ClaimProbatoryDocumentsJob>(x => x.ExportPDF(context.Message));
		}
	}
#pragma warning restore 1998
}
