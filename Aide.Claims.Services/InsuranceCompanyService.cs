using Aide.Core.CustomExceptions;
using Aide.Claims.Domain.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Net;
using System.Linq;
using Newtonsoft.Json;

namespace Aide.Claims.Services
{
    public interface IInsuranceCompanyService
	{
		Task<IEnumerable<InsuranceCompany>> GetAllInsuranceCompanies();
		Task<IEnumerable<InsuranceCompany>> GetInsuranceCompanyListByInsuranceCompanyIds(int[] insuranceCompanyIds);
		Task<InsuranceCompany> GetInsuranceCompanyById(int companyId);
	}

	public class InsuranceCompanyService : IInsuranceCompanyService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		private readonly InsuranceCompanyServiceConfig _serviceConfig;
		private const string _defaultMediaType = "application/json";
        

        #endregion

        #region Constructor

        public InsuranceCompanyService(HttpClient httpClient, InsuranceCompanyServiceConfig serviceConfig)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<InsuranceCompany>> GetAllInsuranceCompanies()
		{
			var endpoint = _serviceConfig.Endpoints["GetAllInsuranceCompanies"];
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
			return JsonConvert.DeserializeObject<IEnumerable<InsuranceCompany>>(responseContent);
		}

		public async Task<IEnumerable<InsuranceCompany>> GetInsuranceCompanyListByInsuranceCompanyIds(int[] insuranceCompanyIds)
		{
			if (insuranceCompanyIds == null) throw new ArgumentNullException(nameof(insuranceCompanyIds));
			var payload = new StringContent(JsonConvert.SerializeObject(insuranceCompanyIds), Encoding.UTF8, _defaultMediaType);
			var endpoint = _serviceConfig.Endpoints["GetInsuranceCompanyListByInsuranceCompanyIds"];
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
            return JsonConvert.DeserializeObject<IEnumerable<InsuranceCompany>>(responseContent);
        }

		public async Task<InsuranceCompany> GetInsuranceCompanyById(int companyId)
        {
			var insuranceCompanyIds = new int[] { companyId };
			var companies = await GetInsuranceCompanyListByInsuranceCompanyIds(insuranceCompanyIds).ConfigureAwait(false);
			return companies.FirstOrDefault();
        }

		#endregion

		#region Local classes

		public class InsuranceCompanyServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
		}

		#endregion
	}
}
