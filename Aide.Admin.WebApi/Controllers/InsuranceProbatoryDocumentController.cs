using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aide.Admin.Domain.Enumerations;
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
    public class InsuranceProbatoryDocumentController : ControllerBase
    {
        private readonly ILogger<InsuranceProbatoryDocumentController> _logger;
        private readonly IInsuranceProbatoryDocumentService _insuranceProbatoryDocumentService;
        private readonly IInsuranceExportProbatoryDocumentService _insuranceExportProbatoryDocumentService;
        private readonly IInsuranceCompanyClaimTypeSettingsService _insuranceCompanyClaimTypeSettingsService;

        public InsuranceProbatoryDocumentController(
            ILogger<InsuranceProbatoryDocumentController> logger, 
            IInsuranceProbatoryDocumentService insuranceProbatoryDocumentService, 
            IInsuranceExportProbatoryDocumentService insuranceExportProbatoryDocumentService,
            IInsuranceCompanyClaimTypeSettingsService insuranceCompanyClaimTypeSettingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _insuranceProbatoryDocumentService = insuranceProbatoryDocumentService ?? throw new ArgumentNullException(nameof(insuranceProbatoryDocumentService));
            _insuranceExportProbatoryDocumentService = insuranceExportProbatoryDocumentService ?? throw new ArgumentNullException(nameof(insuranceExportProbatoryDocumentService));
            _insuranceCompanyClaimTypeSettingsService = insuranceCompanyClaimTypeSettingsService ?? throw new ArgumentNullException(nameof(insuranceCompanyClaimTypeSettingsService));
        }

        [HttpGet("insuranceCompany/{insuranceCompanyId}/documents")]
        public async Task<IActionResult> GetByInsuranceId(int insuranceCompanyId)
        {
            if (insuranceCompanyId <= 0) return BadRequest();
			try
			{
                var result = await _insuranceProbatoryDocumentService.GetInsuranceProbatoryDocumentsByInsuranceId(insuranceCompanyId);
                return Ok(result);
            }
			catch (Exception ex)
			{
                _logger.LogError(ex, $"Couldn't get the list of probatory documents for insurance ID {insuranceCompanyId}");
				throw;
			}
        }

        [HttpGet("insuranceCompany/{insuranceCompanyId}/serviceType/{claimTypeId}/documents")]
        public async Task<IActionResult> GetInsuranceProbatoryDocumentsByInsuranceCompanyIdAndClaimTypeId(int insuranceCompanyId, int claimTypeId)
        {
            if (insuranceCompanyId <= 0) return BadRequest();
            if (claimTypeId <= 0) return BadRequest();
            try
            {
                var result = await _insuranceProbatoryDocumentService.GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(insuranceCompanyId, (EnumClaimTypeId)claimTypeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the list of probatory documents for insurance ID {insuranceCompanyId} and claim type ID {claimTypeId}");
                throw;
            }
        }

        [HttpPost("insuranceCompany/{insuranceCompanyId}/serviceType/{claimTypeId}/documents")]
        public async Task <IActionResult> UpsertInsuranceProbatoryDocumentsByInsuranceCompanyIdAndClaimTypeId(int insuranceCompanyId, int claimTypeId, InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest[] request)
		{
            if (insuranceCompanyId <= 0) return BadRequest();
            if (claimTypeId <= 0) return BadRequest();
            if (request == null) return BadRequest();
            try
            {
                await _insuranceProbatoryDocumentService.UpsertInsuranceProbatoryDocuments(insuranceCompanyId, (EnumClaimTypeId)claimTypeId, request);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't upsert the list of probatory documents for insurance ID {insuranceCompanyId} and claim type ID {claimTypeId}");
                throw;
            }
        }

        [HttpGet("insuranceCompany/{insuranceCompanyId}/serviceType/{claimTypeId}/export")]
        public async Task<IActionResult> GetExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(int insuranceCompanyId, int claimTypeId)
        {
            if (insuranceCompanyId <= 0) return BadRequest();
            if (claimTypeId <= 0) return BadRequest();
            try
            {
                var result = await _insuranceExportProbatoryDocumentService.GetExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(insuranceCompanyId, (EnumClaimTypeId)claimTypeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the export list of probatory documents for insurance ID {insuranceCompanyId} and claim type ID {claimTypeId}");
                throw;
            }
        }

        [HttpGet("insuranceCompany/{insuranceCompanyId}/serviceType/{claimTypeId}/export/{exportTypeId}")]
        public async Task<IActionResult> GetCustomizedExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(int insuranceCompanyId, int claimTypeId, EnumExportTypeId exportTypeId)
        {
            if (insuranceCompanyId <= 0) return BadRequest();
            if (claimTypeId <= 0) return BadRequest();
            try
            {
                var result = await _insuranceExportProbatoryDocumentService.GetCustomizedExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(insuranceCompanyId, (EnumClaimTypeId)claimTypeId, exportTypeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the export list of probatory documents for insurance ID {insuranceCompanyId} and claim type ID {claimTypeId}");
                throw;
            }
        }

        [HttpPost("insuranceCompany/{insuranceCompanyId}/serviceType/{claimTypeId}/export/{exportTypeId}")]
        public async Task<IActionResult> UpsertInsuranceExportProbatoryDocumentsByInsuranceCompanyIdAndClaimTypeIdAndExportTypeId(int insuranceCompanyId, int claimTypeId, EnumExportTypeId exportTypeId, InsuranceExportProbatoryDocumentService.InsuranceExportProbatoryDocumentUpsertRequest[] request)
        {
            if (insuranceCompanyId <= 0) return BadRequest();
            if (claimTypeId <= 0) return BadRequest();
            if (exportTypeId <= 0) return BadRequest();
            if (request == null) return BadRequest();
            try
            {
                await _insuranceExportProbatoryDocumentService.UpsertInsuranceExportProbatoryDocuments(insuranceCompanyId, (EnumClaimTypeId)claimTypeId, exportTypeId, request);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't upsert the export list of probatory documents for insurance ID {insuranceCompanyId} and claim type ID {claimTypeId}");
                throw;
            }
        }

        [HttpGet("insuranceCompany/{insuranceCompanyId}/serviceType/{claimTypeId}/export/{exportTypeId}/settings")]
        public async Task<IActionResult> GetInsuranceExportSettings(int insuranceCompanyId, int claimTypeId, EnumExportTypeId exportTypeId)
        {
            if (insuranceCompanyId <= 0) throw new ArgumentOutOfRangeException(nameof(insuranceCompanyId));
            if (claimTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(claimTypeId));
            if (exportTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(exportTypeId));

            var request = new InsuranceExportProbatoryDocumentService.InsuranceExportSettingsRequest
            {
                InsuranceCompanyId = insuranceCompanyId,
                ClaimTypeId = (EnumClaimTypeId)claimTypeId,
                ExportTypeId = exportTypeId
            };

            try
            {
                var result = await _insuranceExportProbatoryDocumentService.GetInsuranceExportSettings(_insuranceCompanyClaimTypeSettingsService, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the export list of probatory documents for insurance ID {insuranceCompanyId} and claim type ID {claimTypeId} and export type ID {exportTypeId}");
                throw;
            }
        }
    }
}