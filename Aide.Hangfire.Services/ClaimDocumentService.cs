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
using Document = Aide.Hangfire.Domain.Objects.Document;

namespace Aide.Hangfire.Services
{
	public interface IClaimDocumentService
	{
		Task<ClaimDocument> AttachDocument(int claimId, Document document, int documentTypeId, bool overwrite, int sortPriority, int groupId);
	}

	public class ClaimDocumentService : IClaimDocumentService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		private readonly ClaimDocumentServiceConfig _serviceConfig;
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

		public ClaimDocumentService(HttpClient httpClient, ClaimDocumentServiceConfig serviceConfig, IUserService userService)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
			_userService = userService ?? throw new ArgumentNullException(nameof(userService));
		}

		#endregion

		#region Methods

		public async Task<ClaimDocument> AttachDocument(int claimId, Document document, int documentTypeId, bool overwrite, int sortPriority, int groupId)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));
			if (document == null) throw new ArgumentNullException(nameof(document));
			if (documentTypeId <= 0) throw new ArgumentException(nameof(documentTypeId));
			if (sortPriority <= -1) throw new ArgumentException(nameof(sortPriority));
			if (groupId <= -1) throw new ArgumentException(nameof(groupId));

			await VerifyAndRenewToken().ConfigureAwait(false);

			var request = new AttachDocumentRequest
			{
				ClaimId = claimId,
				Document = document,
				DocumentTypeId = documentTypeId,
				Overwrite = overwrite,
				SortPriority = sortPriority,
				GroupId = groupId
			};

			var payload = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, _defaultMediaType);
			var endpoint = _serviceConfig.Endpoints["AttachDocument"];
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
			return JsonConvert.DeserializeObject<ClaimDocument>(responseContent);
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

		public class ClaimDocumentServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
			public ServiceCredentialsConfig Credentials { get; set; }
		}

		public class AttachDocumentRequest
		{
			public int ClaimId { get; set; }
			public Document Document { get; set; }
			public int DocumentTypeId { get; set; }
			public bool Overwrite { get; set; }
			public int SortPriority { get; set; }
			public int GroupId { get; set; }
		}

		#endregion
	}
}
