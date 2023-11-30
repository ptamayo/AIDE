using Hangfire;
using Aide.Hangfire.Common.Messages;
using MassTransit;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
    public class ZipFilesMessageConsumer: IConsumer<ZipFilesMessage>
    {
        public async Task Consume(ConsumeContext<ZipFilesMessage> context)
        {
            BackgroundJob.Enqueue<ZipFilesManagementJob>(x => x.ZipFiles(context.Message));
        }
    }
#pragma warning restore 1998
}
