using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Aide.Notifications.WebApi.Hubs;
using Aide.Notifications.Domain.Enumerations;
using Aide.Notifications.Domain.Objects;
using Aide.Notifications.Services;
using Aide.Notifications.WebApi.Models;

namespace Aide.Notifications.WebApi.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IHubContext<NotificationHub> _hubContext;
		private readonly INotificationService _notificationService;

		public HomeController(ILogger<HomeController> logger, IHubContext<NotificationHub> hubContext, INotificationService notificationService)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(_logger));
			_hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
			_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
		}

		public IActionResult Index()
		{
			return View();
		}

		//public IActionResult Privacy()
		public async Task<IActionResult> Privacy()
		{
			//await _hubContext.Clients.All.SendAsync("Broadcast", "Hello World!");
			//await _hubContext.Clients.User("admin@aideguru.com").SendAsync("PrivateMessage", "Hello World!");

			var groupNotification = new Notification
			{
				Type = EnumNotificationTypeId.GroupMessage,
				Source = "System",
				Target = EnumUserRoleId.Admin.ToString(),
				MessageType = "Test",
				Message = "{ \"title\": \"Title X\", \"content\": \"Hello World!\" }"
			};
			await _hubContext.Clients
				.Group(groupNotification.Target)
				.SendAsync(groupNotification.Type.ToString(), groupNotification);

			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

		[HttpPost]
		public async Task<IActionResult> Index([FromBody]Notification notification)
		{
			if (notification == null) return BadRequest();

			var result = await _notificationService.InsertNotification(notification);

			if (result.Id == 0)
			{
				_logger.LogError("Couldn't persist the notification so that it won't be propagated to the Hub.");
				throw new OperationCanceledException("Couldn't persist the notification so that it won't be propagated to the Hub.");
			}

			switch (notification.Type)
			{
				case EnumNotificationTypeId.Broadcast:
					await _hubContext.Clients
						.All
						.SendAsync(notification.Type.ToString(), notification);
					break;
				case EnumNotificationTypeId.GroupMessage:
					await _hubContext.Clients
						.Group(notification.Target)
						.SendAsync(notification.Type.ToString(), notification);
					break;
				case EnumNotificationTypeId.PrivateMessage:
					await _hubContext.Clients
						.User(notification.Target)
						.SendAsync(notification.Type.ToString(), notification);
					break;
			}

			return Ok(result);
		}
	}
}
