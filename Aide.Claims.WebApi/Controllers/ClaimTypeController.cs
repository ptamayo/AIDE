using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aide.Core.CustomExceptions;
using Aide.Claims.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aide.Claims.WebApi.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class ClaimTypeController : ControllerBase
    {
        private readonly ILogger<ClaimTypeController> _logger;
        private readonly IClaimTypeService _claimTypeService;

        public ClaimTypeController(ILogger<ClaimTypeController> logger, IClaimTypeService claimTypeService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _claimTypeService = claimTypeService ?? throw new ArgumentNullException(nameof(claimTypeService));
        }

        // GET: api/ClaimType
        [HttpGet]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> Get()
        {
            try
            {
                var result = await _claimTypeService.GetAllClaimTypes();
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the full list of claim types.");
                throw;
            }
        }

        [HttpGet("{id}")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest();
            try
            {
                var result = await _claimTypeService.GetClaimTypeById(id);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the claim type bt ID {id}.");
                throw;
            }
        }

        [HttpPost("listByIds")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> GetClaimTypeListByIds(int[] claimTypeIds)
        {
            if (claimTypeIds == null) return BadRequest();
            if (!claimTypeIds.Any()) return BadRequest();

            try
            {
                var result = await _claimTypeService.GetClaimTypeListByIds(claimTypeIds);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the list of claim types.");
                throw;
            }
        }
    }
}