using Aide.Core.CustomExceptions;
using Aide.Core.Interfaces;
using Aide.Hangfire.Domain.Objects;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Aide.Hangfire.Services
{
	public interface IUserService
	{
		bool IsTokenExpired(string token);
		Task<UserAuth> Authenticate(string usr, string psw);
	}

	public class UserService : IUserService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		private readonly UserServiceConfig _serviceConfig;
		private const string _defaultMediaType = "application/json";
        
        private IJwtSecurityTokenHandlerAdapter _tokenHandler;
		private const int GapInMinutes = 5;

		#endregion

		#region Constructor

		public UserService(HttpClient httpClient, UserServiceConfig serviceConfig, IJwtSecurityTokenHandlerAdapter tokenHandler)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
			_tokenHandler = tokenHandler ?? throw new ArgumentNullException(nameof(tokenHandler));
		}

		#endregion

		#region Methods

		public bool IsTokenExpired(string token)
		{
			if (string.IsNullOrWhiteSpace(token)) return true;
			var jwtSecurityToken = _tokenHandler.ReadJwtToken(token);
			if (DateTime.UtcNow >= jwtSecurityToken.ValidFrom && DateTime.UtcNow <= jwtSecurityToken.ValidTo.AddMinutes(-GapInMinutes))
			{
				return false;
			}
			return true;
		}

		public async Task<UserAuth> Authenticate(string usr, string psw)
		{
			if (string.IsNullOrEmpty(usr)) throw new ArgumentNullException(nameof(usr));
			if (string.IsNullOrEmpty(psw)) throw new ArgumentNullException(nameof(psw));

			var request = new AuthenticateModel
			{
				Email = usr,
				Password = psw
			};
			var payload = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, _defaultMediaType);
			var endpoint = _serviceConfig.Endpoints["Authenticate"];
			var response = await _httpClient.PostAsync(endpoint, payload).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();
			if (!response.IsSuccessStatusCode)
			{
				throw new EndpointRequestCustomizedException($"Got {response.StatusCode} from endpoint {response.RequestMessage.RequestUri}");
			}
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new NonExistingRecordCustomizedException();
			}
			var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			return JsonConvert.DeserializeObject<UserAuth>(responseContent);
		}

		#endregion

		#region Local classes

		public class UserServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
		}

		private class AuthenticateModel
		{
			public string Email { get; set; }
			public string Password { get; set; }
		}

		#endregion
	}
}
