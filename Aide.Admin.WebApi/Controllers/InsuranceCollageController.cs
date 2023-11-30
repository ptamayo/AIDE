using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Services;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Aide.Admin.WebApi.Controllers
{
	[Authorize]
	[Route("[controller]")]
	[ApiController]
	public class InsuranceCollageController : ControllerBase
	{
		private readonly ILogger _logger;
		private readonly IInsuranceCollageService _insuranceCollageService;

		public InsuranceCollageController(ILogger<InsuranceCollageController> logger, IInsuranceCollageService insuranceCollageService)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_insuranceCollageService = insuranceCollageService ?? throw new ArgumentNullException(nameof(insuranceCollageService));
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
                var page = await _insuranceCollageService.GetAllInsuranceCollages(pagingSettings, filters);
                return Ok(page);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the list of collages by Company ID {pagingAndFiltering.CompanyId}");
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest();
            try
            {
                var result = await _insuranceCollageService.GetInsuranceCollageById(id);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the collage ID {id}.");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] InsuranceCollage collage)
        {
            if (collage == null) return BadRequest();
            try
            {
                await _insuranceCollageService.InsertInsuranceCollage(collage);
                return Ok(collage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't create the collage.");
                throw;
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] InsuranceCollage collage)
        {
            if (collage == null) return BadRequest();
            if (collage.Id <= 0) return BadRequest();
            if (collage.ProbatoryDocuments == null) return BadRequest();
            try
            {
                await _insuranceCollageService.UpdateInsuranceCollage(collage);
                return Ok(collage);
            }
            catch (NonExistingRecordCustomizedException ex)
            {
                _logger.LogWarning(ex, $"Cannot update because the collage ID {collage.Id} has not been found.");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't update the collage ID {collage.Id}.");
                throw;
            }
        }

		[HttpDelete("{collageId}/delete")]
		public async Task<IActionResult> Delete([FromRoute] int collageId)
		{
			if (collageId <= 0) return BadRequest();
			try
			{
				await _insuranceCollageService.DeleteInsuranceCollage(collageId);
				return Ok();
			}
			catch (NonExistingRecordCustomizedException ex)
			{
				_logger.LogWarning(ex, $"Cannot delete because the collage ID {collageId} has not been found.");
				return NotFound();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't delete the collage ID {collageId}.");
				throw;
			}
		}

		//[HttpPost("{companyId}/collages")]
		//public async Task<IActionResult> GetCollageListByCompanyId([FromRoute] int companyId)
		//{
		//    if (companyId <= 0) throw new ArgumentException(nameof(companyId));

		//    try
		//    {
		//        var result = await _insuranceCollageService.GetInsuranceCollageListByInsuranceCompanyId(companyId);
		//        return Ok(result);
		//    }
		//    catch (Exception ex)
		//    {
		//        _logger.LogError(ex, $"Couldn't get the list of collages by Company ID {companyId}");
		//        throw;
		//    }
		//}

		[HttpPost("{companyId}/{claimTypeId}/collage")]
        public async Task<IActionResult> GetCollageListByCompanyIdAndClaimTypeId([FromRoute] int companyId, [FromRoute] EnumClaimTypeId claimTypeId)
        {
            if (companyId <= 0) throw new ArgumentException(nameof(companyId));
            if (claimTypeId <= 0) throw new ArgumentException(nameof(claimTypeId));

            try
            {
                var result = await _insuranceCollageService.GetInsuranceCollageListByInsuranceCompanyIdAndClaimTypeId(companyId, claimTypeId);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException ex)
			{
                _logger.LogWarning(ex, $"Couldn't find a collage for Company ID {companyId} and Claim Type ID {claimTypeId}");
                return NotFound();
			}
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the list of collages by Company ID {companyId} and Claim Type ID {claimTypeId}");
                throw;
            }
        }

        #region Local classes

        public class PagingAndFiltering
        {
            public int PageSize { get; set; }
            public int PageNumber { get; set; }
            public string Keywords { get; set; }
            public int? CompanyId { get; set; }
        }

        #endregion
    }

    #region Extension methods

    public static class InsuranceCollageControllerPagingAndFilteringExtensions
    {
        public static PagingSettings ToPagingSettings(this InsuranceCollageController.PagingAndFiltering pagingAndFiltering)
        {
            return new PagingSettings
            {
                PageNumber = pagingAndFiltering.PageNumber,
                PageSize = pagingAndFiltering.PageSize
            };
        }

        public static InsuranceCollageService.Filters ToFilters(this InsuranceCollageController.PagingAndFiltering pagingAndFiltering)
        {
            return new InsuranceCollageService.Filters
            {
                Keywords = pagingAndFiltering.Keywords.DecodeUTF8().CleanDoubleWhiteSpaces(),
                CompanyId = pagingAndFiltering.CompanyId
            };
        }
    }

    #endregion
}
