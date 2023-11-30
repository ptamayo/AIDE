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
	public interface IClaimProbatoryDocumentService
	{
		Task<ClaimProbatoryDocumentMedia> AttachProbatoryDocument(ClaimProbatoryDocumentService.AttachProbatoryDocumentRequest request);
    }

	public class ClaimProbatoryDocumentService : IClaimProbatoryDocumentService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		private readonly ClaimProbatoryDocumentServiceConfig _serviceConfig;
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

		public ClaimProbatoryDocumentService(HttpClient httpClient, ClaimProbatoryDocumentServiceConfig serviceConfig, IUserService userService)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
			_userService = userService ?? throw new ArgumentNullException(nameof(userService));
		}

        #endregion

        #region Methods

        public async Task<ClaimProbatoryDocumentMedia> AttachProbatoryDocument(AttachProbatoryDocumentRequest request)
        {
			if (request == null) throw new ArgumentNullException(nameof(request));
			if (request.ClaimProbatoryDocumentId <= 0) throw new ArgumentOutOfRangeException(nameof(request.ClaimProbatoryDocumentId));
			if (request.Media == null) throw new ArgumentNullException(nameof(request.Media));

			await VerifyAndRenewToken().ConfigureAwait(false);

            var payload = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, _defaultMediaType);
            var endpoint = _serviceConfig.Endpoints["AttachProbatoryDocument"];
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
			return JsonConvert.DeserializeObject<ClaimProbatoryDocumentMedia>(responseContent);
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

		public class ClaimProbatoryDocumentServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
			public ServiceCredentialsConfig Credentials { get; set; }
		}

		public class AttachProbatoryDocumentRequest
		{
			public int ClaimProbatoryDocumentId { get; set; }
			public Media Media { get; set; }
		}

		#endregion
	}
}
