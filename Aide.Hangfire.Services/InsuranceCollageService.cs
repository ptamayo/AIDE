using Aide.Core.CustomExceptions;
using Aide.Hangfire.Domain.Enumerations;
using Aide.Hangfire.Domain.Objects;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Aide.Hangfire.Services
{
	public interface IInsuranceCollageService
	{
		Task<IEnumerable<InsuranceCollage>> GetInsuranceCollageListByInsuranceCompanyIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId);
	}

	public class InsuranceCollageService : IInsuranceCollageService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		private readonly InsuranceCollageServiceConfig _serviceConfig;
		private const string _defaultMediaType = "application/json";
        
        private readonly IUserService _userService;
		private static string _token;
		private object _sync = new object();

		private string Token
		{
			get
			{
				lock (_sync)
				{
					return _token;
				}
			}
			set
			{
				lock (_sync)
				{
					_token = value;
				}
			}
		}

		#endregion

		#region Constructor

		public InsuranceCollageService(HttpClient httpClient, InsuranceCollageServiceConfig serviceConfig, IUserService userService)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
			_userService = userService ?? throw new ArgumentNullException(nameof(userService));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<InsuranceCollage>> GetInsuranceCollageListByInsuranceCompanyIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId)
		{
			if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));
			if (claimTypeId <= 0) throw new ArgumentException(nameof(claimTypeId));

			await VerifyAndRenewToken().ConfigureAwait(false);

			var payload = new StringContent(JsonConvert.SerializeObject(string.Empty), Encoding.UTF8, _defaultMediaType);
			var endpoint = String.Format(_serviceConfig.Endpoints["GetCollageListByCompanyIdAndClaimTypeId"], insuranceCompanyId, (int)claimTypeId);
			var response = await _httpClient.PostAsync(endpoint, payload).ConfigureAwait(false);
			//response.EnsureSuccessStatusCode(); // This is very important: In case of 404 Not Found this line will break the execution
			if (!response.IsSuccessStatusCode)
			{
				throw new EndpointRequestCustomizedException($"Got {response.StatusCode} from endpoint {response.RequestMessage.RequestUri}");
			}
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new NonExistingRecordCustomizedException();
			}
			var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			return JsonConvert.DeserializeObject<IEnumerable<InsuranceCollage>>(responseContent);
		}

		private async Task VerifyAndRenewToken()
		{
			if (_userService.IsTokenExpired(Token))
			{
				var userAuth = await _userService.Authenticate(_serviceConfig.Credentials.Username, _serviceConfig.Credentials.HashedPsw).ConfigureAwait(false);
				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.Token);
				Token = userAuth.Token;
			}
			if (_httpClient.DefaultRequestHeaders.Authorization == null)
			{
				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
			}
		}

		#endregion

		#region Local classes

		public class InsuranceCollageServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
			public ServiceCredentialsConfig Credentials { get; set; }
		}

		#endregion
	}
}
