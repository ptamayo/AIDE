using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aide.Hangfire.Worker
{
	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> _logger;

		public Worker(ILogger<Worker> logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public override async Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Worker started");
			await base.StartAsync(cancellationToken);
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			await base.StopAsync(cancellationToken);
			_logger.LogInformation("Worker stopped");
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(1000, stoppingToken);
			}
		}

		public override void Dispose() { }
	}
}
