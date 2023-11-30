using Aide.Core.CustomExceptions;
using Aide.Hangfire.Domain.Enumerations;
using Aide.Hangfire.Domain.Objects;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Aide.Hangfire.Services
{
    public interface IInsuranceProbatoryDocumentService
    {
        Task<InsuranceExportSettings> GetInsuranceExportSettings(int insuranceCompanyId, EnumClaimTypeId claimTypeId, EnumExportTypeId exportTypeId);
    }

    public class InsuranceProbatoryDocumentService : IInsuranceProbatoryDocumentService
    {
        #region Properties

        private readonly HttpClient _httpClient;
        private readonly InsuranceProbatoryDocumentServiceConfig _serviceConfig;
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

        public InsuranceProbatoryDocumentService(HttpClient httpClient, InsuranceProbatoryDocumentServiceConfig serviceConfig, IUserService userService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        #endregion

        #region Methods

        public async Task<InsuranceExportSettings> GetInsuranceExportSettings(int insuranceCompanyId, EnumClaimTypeId claimTypeId, EnumExportTypeId exportTypeId)
        {
            if (insuranceCompanyId <= 0) throw new ArgumentOutOfRangeException(nameof(insuranceCompanyId));
            if (claimTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(claimTypeId));
            if (exportTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(exportTypeId));

            await VerifyAndRenewToken().ConfigureAwait(false);

            var endpoint = String.Format(_serviceConfig.Endpoints["GetInsuranceExportSettings"], insuranceCompanyId, (int)claimTypeId, (int)exportTypeId);
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
            return JsonConvert.DeserializeObject<InsuranceExportSettings>(responseContent);
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

        public class InsuranceProbatoryDocumentServiceConfig
        {
            public Dictionary<string, string> Endpoints { get; set; }
            public ServiceCredentialsConfig Credentials { get; set; }
        }

        //public class AttachDocumentRequest
        //{
        //	public int ClaimId { get; set; }
        //	public Document Document { get; set; }
        //	public int DocumentTypeId { get; set; }
        //	public bool Overwrite { get; set; }
        //	public int SortPriority { get; set; }
        //	public int GroupId { get; set; }
        //}

        #endregion
    }
}
