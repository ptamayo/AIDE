using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aide.Admin.WebApi.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class InsuranceCompanyController : ControllerBase
    {
        private readonly ILogger<InsuranceCompanyController> _logger;
        private readonly IInsuranceCompanyService _insuranceCompanyService;
        private readonly IInsuranceProbatoryDocumentService _insuranceProbatoryDocumentService;
        private readonly IInsuranceCompanyClaimTypeSettingsService _insuranceCompanyClaimTypeSettingsService;
        private readonly IUserService _userService;

        public InsuranceCompanyController(
            ILogger<InsuranceCompanyController> logger, 
            IInsuranceCompanyService insuranceCompanyService, 
            IInsuranceProbatoryDocumentService insuranceProbatoryDocumentService,
            IInsuranceCompanyClaimTypeSettingsService insuranceCompanyClaimTypeSettingsService,
            IUserService userService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _insuranceCompanyService = insuranceCompanyService ?? throw new ArgumentNullException(nameof(insuranceCompanyService));
            _insuranceProbatoryDocumentService = insuranceProbatoryDocumentService ?? throw new ArgumentNullException(nameof(insuranceProbatoryDocumentService));
            _insuranceCompanyClaimTypeSettingsService = insuranceCompanyClaimTypeSettingsService ?? throw new ArgumentNullException(nameof(insuranceCompanyClaimTypeSettingsService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var result = await _insuranceCompanyService.GetAllInsuranceCompanies(EnumViewDetail.Minimum);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet("list/enabled")]
        public async Task<IActionResult> GetEnabled()
        {
            try
            {
                var result = await _insuranceCompanyService.GetAllEnabledInsuranceCompanies(EnumViewDetail.Extended);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetList(PagingAndFiltering pagingAndFiltering)
        {
            if (pagingAndFiltering == null) return BadRequest();
            if (pagingAndFiltering.PageNumber < 1 || pagingAndFiltering.PageSize < 1) return BadRequest();

            var pagingSettings = pagingAndFiltering.ToPagingSettings();
            var filters = pagingAndFiltering.ToFilters();

            try
            {
                var page = await _insuranceCompanyService.GetAllInsuranceCompanies(pagingSettings, filters);
                return Ok(page);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get the list of insurance companies.");
                throw;
            }
        }

        /// <summary>
		/// This is for INTERNAL/PRIVATE use only and SHOULD NOT be exposed to the PUBLIC in the API GATEWAY
		/// </summary>
		/// <param name="insuranceCompanyIds">insuranceCompanyIds</param>
		/// <returns></returns>
		[HttpPost("listByIds")]
        public async Task<IActionResult> GetListByInsuranceCompanyIds(int[] insuranceCompanyIds)
        {
            if (insuranceCompanyIds == null) return BadRequest();
            if (insuranceCompanyIds.Length == 0) return BadRequest();

            try
            {
                var result = await _insuranceCompanyService.GetInsuranceCompanyListByInsuranceCompanyIds(insuranceCompanyIds);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get the list of insurance companies.");
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest();
            try
            {
                var result = await _insuranceCompanyService.GetInsuranceCompanyById(id);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the insurance company ID {id}.");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Insert(InsuranceCompany insuranceCompany)
        {
            if (insuranceCompany == null) return BadRequest();
            try
            {
                await _insuranceCompanyService.InsertInsuranceCompany(insuranceCompany);
                return Ok(insuranceCompany);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't create the insurance company.");
                throw;
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] InsuranceCompany insuranceCompany)
        {
            if (insuranceCompany == null) return BadRequest();
            try
            {
                await _insuranceCompanyService.UpdateInsuranceCompany(insuranceCompany);
                return Ok(insuranceCompany);
            }
            catch (NonExistingRecordCustomizedException ex)
            {
                _logger.LogWarning(ex, $"Cannot update because the insurance company ID {insuranceCompany.Id} has not been found.");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't update the insurance company ID {insuranceCompany.Id}.");
                throw;
            }
        }

        [HttpGet("{id}/documents/{claimTypeId}")]
        public async Task<IActionResult> GetProbatoryDocumentsByInsuranceCompanyIdAndClaimTypeId(int id, EnumClaimTypeId claimTypeId)
        {
            if (id <= 0) return BadRequest();

            try
            {
                var result = await _insuranceProbatoryDocumentService.GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(id, claimTypeId);
                return Ok(result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost("{companyId}/users")]
        public async Task<IActionResult> GetUserListByCompanyId([FromRoute] int companyId, [FromBody] UserController.PagingAndFiltering pagingAndFiltering)
        {
            if (companyId <= 0) return BadRequest();
            if (pagingAndFiltering == null) return BadRequest();
            if (pagingAndFiltering.PageNumber < 1 || pagingAndFiltering.PageSize < 1) return BadRequest();

            try
            {
                var filters = pagingAndFiltering.ToFilters();
                var pagingSettings = pagingAndFiltering.ToPagingSettings();
                var page = await _userService.GetAllUsers(pagingSettings, filters, EnumViewDetail.Minimum);
                return Ok(page);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the list of users by Company ID {companyId} and Company Type {EnumCompanyTypeId.Insurance}");
                throw;
            }
        }

        [HttpPost("{companyId}/serviceType/{claimTypeId}/settings")]
        public async Task<IActionResult> PostInsuranceClaimTypeSettings([FromRoute] int companyId, EnumClaimTypeId claimTypeId, [FromBody] InsuranceClaimTypeSettingsPostRequest settings)
        {
            if (companyId <= 0) return BadRequest();
            if (claimTypeId <= 0) return BadRequest();
            if (settings == null) return BadRequest();

            var claimTypeSettings = new InsuranceCompanyClaimTypeSettings
            {
                InsuranceCompanyId = companyId,
                ClaimTypeId = claimTypeId,
                IsDepositSlipRequired = settings.IsDepositSlipRequired,
                IsExportingCustomizedDocsToPdf = settings.IsExportingCustomizedDocsToPdf,
                IsExportingCustomizedDocsToZip = settings.IsExportingCustomizedDocsToZip,
                IsClaimServiceEnabled = settings.IsClaimServiceEnabled
            };

            try
            {
                await _insuranceCompanyClaimTypeSettingsService.Upsert(claimTypeSettings);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't save the settings for claim type {claimTypeId}");
                throw;
            }
        }

        #region Local classes

        public class PagingAndFiltering
        {
            public int PageSize { get; set; }
            public int PageNumber { get; set; }
            public string Keywords { get; set; }
        }

        public class InsuranceClaimTypeSettingsPostRequest
        {
            public bool? IsClaimServiceEnabled { get; set; }
            public bool? IsDepositSlipRequired { get; set; }
            public bool? IsExportingCustomizedDocsToPdf { get; set; }
            public bool? IsExportingCustomizedDocsToZip { get; set; }
        }

        #endregion
    }

    #region Extension methods

    public static class InsuranceCompanyControllerPagingAndFilteringExtensions
    {
        public static PagingSettings ToPagingSettings(this InsuranceCompanyController.PagingAndFiltering pagingAndFiltering)
        {
            return new PagingSettings
            {
                PageNumber = pagingAndFiltering.PageNumber,
                PageSize = pagingAndFiltering.PageSize
            };
        }

        public static InsuranceCompanyService.Filters ToFilters(this InsuranceCompanyController.PagingAndFiltering pagingAndFiltering)
        {
            return new InsuranceCompanyService.Filters
            {
                Keywords = pagingAndFiltering.Keywords.DecodeUTF8().CleanDoubleWhiteSpaces()
            };
        }
    }

    #endregion
}