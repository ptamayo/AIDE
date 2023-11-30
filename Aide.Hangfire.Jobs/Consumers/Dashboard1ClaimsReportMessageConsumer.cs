using Hangfire;
using Aide.Hangfire.Common.Messages;
using MassTransit;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs.Consumers
{
#pragma warning disable 1998
    public class Dashboard1ClaimsReportMessageConsumer:IConsumer<Dashboard1ClaimsReportMessage>
    {
        public async Task Consume(ConsumeContext<Dashboard1ClaimsReportMessage> context)
        {
            BackgroundJob.Enqueue<ReportJob>(x => x.Dashboard1ClaimsReport(context.Message));
        }
    }
#pragma warning restore 1998
}
