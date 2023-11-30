using Aide.Admin.Domain.Objects;
using Aide.Core.CustomExceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Aide.Admin.Services
{
	public interface IClaimTypeService
	{
		Task<IEnumerable<ClaimType>> GetAllClaimTypes();
	}

	public class ClaimTypeService : IClaimTypeService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		private readonly ClaimTypeServiceConfig _serviceConfig;
        //private const string _defaultMediaType = "application/json";

        #endregion

        #region Constructor

        public ClaimTypeService(HttpClient httpClient, ClaimTypeServiceConfig serviceConfig)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<ClaimType>> GetAllClaimTypes()
		{
			var endpoint = _serviceConfig.Endpoints["GetAllClaimTypes"];
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
			return JsonConvert.DeserializeObject<IEnumerable<ClaimType>>(responseContent);
		}

		#endregion

		#region Local classes

		public class ClaimTypeServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
		}

		#endregion
	}
}
