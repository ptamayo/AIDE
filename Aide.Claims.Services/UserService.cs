using Aide.Core.CustomExceptions;
using Aide.Claims.Domain.Enumerations;
using Aide.Claims.Domain.Objects;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;

namespace Aide.Claims.Services
{
	public interface IUserService
	{
		Task<IEnumerable<User>> GetUserListByUserIds(int[] userIds);
		object ReadKeyFromJwtToken(string token, string key);
		User BuildUserFromJwtToken(string token);
	}

	public class UserService : IUserService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		private readonly UserServiceConfig _serviceConfig;
		private const string _defaultMediaType = "application/json";
        

        #endregion

        #region Constructor

        public UserService(HttpClient httpClient, UserServiceConfig serviceConfig)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<User>> GetUserListByUserIds(int[] userIds)
		{
			if (userIds == null || !userIds.Any()) throw new ArgumentException(nameof(userIds));
			var payload = new StringContent(JsonConvert.SerializeObject(userIds), Encoding.UTF8, _defaultMediaType);
			var endpoint = _serviceConfig.Endpoints["GetUserListByUserIds"];
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
			return JsonConvert.DeserializeObject<IEnumerable<User>>(responseContent);
		}

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

		// NEED REVISIT: Must moved out here and communicate with Aide.Admin.WebApi instead
		public User BuildUserFromJwtToken(string token)
		{
			if (string.IsNullOrWhiteSpace(token)) throw new ArgumentNullException(nameof(token));
			token = token.Replace("Bearer", "").Trim();
			var user = new User
			{
				Id = Convert.ToInt32(ReadKeyFromJwtToken(token, "nameid")),
				RoleId = (EnumUserRoleId)Convert.ToInt32(ReadKeyFromJwtToken(token, "role")),
				FirstName = ReadKeyFromJwtToken(token, "given_name").ToString(),
				LastName = ReadKeyFromJwtToken(token, "family_name").ToString(),
				Email = ReadKeyFromJwtToken(token, "email").ToString(),
				DateCreated = Convert.ToDateTime(ReadKeyFromJwtToken(token, "dateCreated")),
				DateLogout = Convert.ToDateTime(ReadKeyFromJwtToken(token, "dateLogout"))
			};

			// Dev Notes: Need revisit. This same logic exists in the client. Need re-think the approach to avoid have this logic repeated in the front-end
			// For further details see auth.service.ts in the client.
			var companyTypeId = EnumCompanyTypeId.Unknown;
			switch (user.RoleId)
			{
				case EnumUserRoleId.InsuranceReadOnly:
					companyTypeId = EnumCompanyTypeId.Insurance;
					break;
				case EnumUserRoleId.WsAdmin:
				case EnumUserRoleId.WsOperator:
					companyTypeId = EnumCompanyTypeId.Store;
					break;
			}

			var companies = new List<UserCompany>();
			if (!string.IsNullOrWhiteSpace(ReadKeyFromJwtToken(token, "companies").ToString()))
			{
				var companiesString = Convert.ToString(ReadKeyFromJwtToken(token, "companies"));
				foreach (var c in companiesString.Split(","))
				{
					var userCompany = new UserCompany
					{
						UserId = user.Id,
						CompanyId = Convert.ToInt32(c),
						CompanyTypeId = companyTypeId
					};
					companies.Add(userCompany);
				}
			}
			user.Companies = companies;

			return user;
		}

		#endregion

		#region Local classes

		public class UserServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
		}

		#endregion
	}
}
