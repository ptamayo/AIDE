using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aide.Core.Cloud.Azure.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aide.Admin.WebApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class WeatherForecastController : ControllerBase
	{
		private readonly IBusService _bus;
		private readonly AppSettings _appSettings;
		private const string QueueName = "plk_queuex";

		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		private readonly ILogger<WeatherForecastController> _logger;

		public WeatherForecastController(ILogger<WeatherForecastController> logger, IBusService bus, AppSettings appSettings)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
		}

		[HttpGet]
		public async Task<IEnumerable<WeatherForecast>> Get()
		{
			// Below you can test the service bus
			var queueUrl = _appSettings.ServiceBusConfig.Queue[QueueName];
			var endpoint = await _bus.GetSendEndpoint(queueUrl);
			await endpoint.Send<TestMessage>(new { Content = "Hello World!" });

			var rng = new Random();
			return await Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
			{
				Date = DateTime.Now.AddDays(index),
				TemperatureC = rng.Next(-20, 55),
				Summary = Summaries[rng.Next(Summaries.Length)]
			})
			.ToArray());
		}
	}
}
