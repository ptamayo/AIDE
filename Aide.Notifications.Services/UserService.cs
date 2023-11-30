using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
//using System.Net.Http;

namespace Aide.Notifications.Services
{
	public interface IUserService
	{
		object ReadKeyFromJwtToken(string token, string key);

	}

	public class UserService : IUserService
	{
		#region Properties

		//private readonly HttpClient _httpClient;
		//private readonly UserServiceConfig _serviceConfig;
		//private const string _defaultMediaType = "application/json";
		//private string _symmetricSecurityKey;

		#endregion

		#region Constructor

		public UserService() //HttpClient httpClient, UserServiceConfig serviceConfig
		{
			//_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			//_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
		}

		#endregion

		#region Methods

		// NEED REVISIT: Must moved out here and communicate with Aide.Admin.WebApi instead
		public object ReadKeyFromJwtToken(string token, string key)
		{
			if (string.IsNullOrWhiteSpace(token)) throw new ArgumentNullException(nameof(token));
			if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var jwtSecurityToken = tokenHandler.ReadJwtToken(token);
				return jwtSecurityToken.Payload[key];
			}
			catch (KeyNotFoundException)
			{
				return null;
			}
			catch (Exception)
			{
				throw;
			}
		}

		#endregion

		#region Local classes

		//public class UserServiceConfig
		//{
		//	public string BaseAddress { get; set; }
		//	public Dictionary<string, string> Endpoints { get; set; }
		//}

		#endregion
	}
}
