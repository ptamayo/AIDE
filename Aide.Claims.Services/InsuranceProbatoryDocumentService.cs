using Aide.Claims.Domain.Enumerations;
using Aide.Claims.Domain.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Aide.Core.CustomExceptions;
using Newtonsoft.Json;

namespace Aide.Claims.Services
{
	public interface IInsuranceProbatoryDocumentService
	{
		Task<IEnumerable<InsuranceProbatoryDocument>> GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId);
	}

	public class InsuranceProbatoryDocumentService : IInsuranceProbatoryDocumentService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		public readonly InsuranceProbatoryDocumentServiceConfig _serviceConfig;
        //private const string _defaultMediaType = "application/json";
        

        #endregion

        #region Constructor

        public InsuranceProbatoryDocumentService(HttpClient httpClient, InsuranceProbatoryDocumentServiceConfig serviceConfig)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<InsuranceProbatoryDocument>> GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId)
		{
			if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));
			var endpoint = String.Format(_serviceConfig.Endpoints["GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId"], insuranceCompanyId, claimTypeId);
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
			return JsonConvert.DeserializeObject<IEnumerable<InsuranceProbatoryDocument>>(responseContent);
		}

		#endregion

		#region Local classes

		public class InsuranceProbatoryDocumentServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
		}

		#endregion
	}
}
