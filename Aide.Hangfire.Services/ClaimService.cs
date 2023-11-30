using Aide.Core.CustomExceptions;
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
	public interface IClaimService
	{
		Task<Claim> GetClaimById(int claimId);
		Task<Signature> GetSignatureByClaimId(int claimId);		
		Task<string> RemoveStaledOrders(double thresholdInHours);
		Task InsertClaim(ClaimServiceRequest dto);
		Task UpdateClaim(ClaimServiceRequest dto);
		Task UpdateStatus(int claimId, int statusId);
	}

	public class ClaimService : IClaimService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		private readonly ClaimServiceConfig _serviceConfig;
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

		public ClaimService(HttpClient httpClient, ClaimServiceConfig serviceConfig, IUserService userService)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
			_userService = userService ?? throw new ArgumentNullException(nameof(userService));
		}

		#endregion

		#region Methods

		public async Task<Claim> GetClaimById(int claimId)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));
			await VerifyAndRenewToken().ConfigureAwait(false);
			var endpoint = String.Format(_serviceConfig.Endpoints["GetClaimById"], claimId);
			var response = await _httpClient.GetAsync(endpoint).ConfigureAwait(false);
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
			return JsonConvert.DeserializeObject<Claim>(responseContent);
		}

		public async Task<Signature> GetSignatureByClaimId(int claimId)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));
			await VerifyAndRenewToken().ConfigureAwait(false);
			var endpoint = String.Format(_serviceConfig.Endpoints["GetSignatureByClaimId"], claimId);
			var response = await _httpClient.GetAsync(endpoint).ConfigureAwait(false);
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
			return JsonConvert.DeserializeObject<Signature>(responseContent);
		}

		public async Task<string> RemoveStaledOrders(double thresholdInHours)
		{
			if (thresholdInHours <= 0) throw new ArgumentOutOfRangeException(nameof(thresholdInHours));

			await VerifyAndRenewToken().ConfigureAwait(false);

			var payload = new StringContent(JsonConvert.SerializeObject(thresholdInHours), Encoding.UTF8, _defaultMediaType);
			var endpoint = _serviceConfig.Endpoints["RemoveStaledOrders"];
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
			// Below notice that in this particular case it's not necessary deserialize the response because it's a non-json string
			return responseContent;
		}

		public async Task InsertClaim(ClaimServiceRequest dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			await VerifyAndRenewToken().ConfigureAwait(false);

			var payload = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, _defaultMediaType);
			var endpoint = _serviceConfig.Endpoints["InsertClaim"];
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
		}

		public async Task UpdateClaim(ClaimServiceRequest dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));
			if (dto.Id <= 0) throw new ArgumentOutOfRangeException(nameof(dto.Id));

			await VerifyAndRenewToken().ConfigureAwait(false);

			var payload = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, _defaultMediaType);
			var endpoint = _serviceConfig.Endpoints["UpdateClaim"];
			var response = await _httpClient.PutAsync(endpoint, payload).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();
			if (!response.IsSuccessStatusCode)
			{
				throw new EndpointRequestCustomizedException($"Got {response.StatusCode} from endpoint {response.RequestMessage.RequestUri}");
			}
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new NonExistingRecordCustomizedException();
			}
		}

		public async Task UpdateStatus(int claimId, int statusId)
		{
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			if (statusId <= 0) throw new ArgumentOutOfRangeException(nameof(statusId));

			await VerifyAndRenewToken().ConfigureAwait(false);

			var payload = new StringContent(JsonConvert.SerializeObject(statusId), Encoding.UTF8, _defaultMediaType);
			var endpoint = String.Format(_serviceConfig.Endpoints["UpdateStatus"], claimId);
			var response = await _httpClient.PutAsync(endpoint, payload).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();
			if (!response.IsSuccessStatusCode)
			{
				throw new EndpointRequestCustomizedException($"Got {response.StatusCode} from endpoint {response.RequestMessage.RequestUri}");
			}
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new NonExistingRecordCustomizedException();
			}			
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

		public class ClaimServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
			public ServiceCredentialsConfig Credentials { get; set; }
		}

		#endregion
	}
}
