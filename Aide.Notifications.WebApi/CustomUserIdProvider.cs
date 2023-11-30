using Aide.Notifications.Services;
using Microsoft.AspNetCore.SignalR;
using System;

namespace Aide.Notifications.WebApi
{
	public class CustomUserIdProvider : IUserIdProvider
	{
		private readonly IUserService _userService;
		private readonly AppSettings _appSettings;
		private const string QueryStringToken = "token";
		private const string JwtKeyEmail = "email";

		public CustomUserIdProvider(IUserService userService, AppSettings appSettings)
		{
			_userService = userService ?? throw new ArgumentNullException(nameof(userService));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
		}

		public string GetUserId(HubConnectionContext connection)
		{
			var token = connection.GetHttpContext().Request.Query[QueryStringToken];
			var username = _userService.ReadKeyFromJwtToken(token, JwtKeyEmail);
			if (username != null)
			{
				return username.ToString();
			}
			return null;
		}
	}
}
