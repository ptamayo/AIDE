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
	public interface IStoreService
	{
		Task<IEnumerable<Store>> GetAllStores();
		Task<IEnumerable<Store>> GetStoreListByStoreIds(int[] storeIds);
	}

	public class StoreService : IStoreService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		private readonly StoreServiceConfig _serviceConfig;
		private const string _defaultMediaType = "application/json";
        

        #endregion

        #region Constructor

        public StoreService(HttpClient httpClient, StoreServiceConfig serviceConfig)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<Store>> GetAllStores()
		{
			var endpoint = _serviceConfig.Endpoints["GetAllStores"];
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
			return JsonConvert.DeserializeObject<IEnumerable<Store>>(responseContent);
		}

		public async Task<IEnumerable<Store>> GetStoreListByStoreIds(int[] storeIds)
		{
			if (storeIds == null) throw new ArgumentNullException(nameof(storeIds));
			var payload = new StringContent(JsonConvert.SerializeObject(storeIds), Encoding.UTF8, _defaultMediaType);
			var endpoint = _serviceConfig.Endpoints["GetStoreListByStoreIds"];
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
			return JsonConvert.DeserializeObject<IEnumerable<Store>>(responseContent);
        }

		#endregion

		#region Local classes

		public class StoreServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
		}

		#endregion
	}
}
