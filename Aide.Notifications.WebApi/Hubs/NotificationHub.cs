using Aide.Notifications.Domain.Enumerations;
using Aide.Notifications.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Aide.Notifications.WebApi.Hubs
{
	public class NotificationHub : Hub
	{
		private readonly ILogger<NotificationHub> _logger;
		private readonly IUserService _userService;
		private readonly AppSettings _appSettings;
		private const string QueryStringToken = "token";
		private const string JwtKeyRole = "role";

		public NotificationHub(ILogger<NotificationHub> logger, IUserService userService, AppSettings appSettings)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_userService = userService ?? throw new ArgumentNullException(nameof(userService));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
		}

		public override Task OnConnectedAsync()
		{
			var token = Context.GetHttpContext().Request.Query[QueryStringToken][0];
			if (token != null)
			{
				var userRole = _userService.ReadKeyFromJwtToken(token, JwtKeyRole);
				if (userRole != null)
				{
					var groupName = ((EnumUserRoleId)Convert.ToInt16(userRole)).ToString();
					Groups.AddToGroupAsync(Context.ConnectionId, groupName);
					_logger.LogInformation($"[{DateTime.Now}] {Context.UserIdentifier} has connected. Connection ID: {Context.ConnectionId}");
				}
			}
			return base.OnConnectedAsync();
		}

		public override Task OnDisconnectedAsync(Exception ex)
		{
			_logger.LogInformation($"[{DateTime.Now}] {Context.UserIdentifier} has disconnected. Connection ID: {Context.ConnectionId}");
			return base.OnDisconnectedAsync(ex);
		}
	}
}
