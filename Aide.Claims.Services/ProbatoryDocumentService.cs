using Aide.Core.CustomExceptions;
using Aide.Claims.Domain.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Net;
using Newtonsoft.Json;

namespace Aide.Claims.Services
{
	public interface IProbatoryDocumentService
	{
		Task<IEnumerable<ProbatoryDocument>> GetProbatoryDocumentListByIds(int[] probatoryDocumentIds);
		Task<ProbatoryDocument> GetProbatoryDocumentById(int probatoryDocumentId);
	}

	public class ProbatoryDocumentService : IProbatoryDocumentService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		public readonly ProbatoryDocumentServiceConfig _serviceConfig;
		private const string _defaultMediaType = "application/json";
		

        #endregion

        #region Constructor

        public ProbatoryDocumentService(HttpClient httpClient, ProbatoryDocumentServiceConfig serviceConfig)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<ProbatoryDocument>> GetProbatoryDocumentListByIds(int[] probatoryDocumentIds)
		{
			if (probatoryDocumentIds == null) throw new ArgumentNullException(nameof(probatoryDocumentIds));
			var payload = new StringContent(JsonConvert.SerializeObject(probatoryDocumentIds), Encoding.UTF8, _defaultMediaType);
			var endpoint = _serviceConfig.Endpoints["GetProbatoryDocumentListByIds"];
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
			return JsonConvert.DeserializeObject<IEnumerable<ProbatoryDocument>>(responseContent);
		}

		public async Task<ProbatoryDocument> GetProbatoryDocumentById(int probatoryDocumentId)
		{
			if (probatoryDocumentId <= 0) throw new ArgumentException(nameof(probatoryDocumentId));
			var endpoint = String.Format(_serviceConfig.Endpoints["GetProbatoryDocumentById"], probatoryDocumentId);
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
			return JsonConvert.DeserializeObject<ProbatoryDocument>(responseContent);
		}

		#endregion

		#region Local classes

		public class ProbatoryDocumentServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
		}

		#endregion
	}
}
