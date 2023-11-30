using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
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
    public class ProbatoryDocumentController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IProbatoryDocumentService _probatoryDocumentService;

        public ProbatoryDocumentController(ILogger<ProbatoryDocumentController> logger, IProbatoryDocumentService probatoryDocumentService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _probatoryDocumentService = probatoryDocumentService ?? throw new ArgumentNullException(nameof(probatoryDocumentService));
        }

        // GET: api/ProbatoryDocument
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _probatoryDocumentService.GetAllProbatoryDocuments();
            return Ok(result);
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
                var page = await _probatoryDocumentService.GetAllProbatoryDocuments(pagingSettings, filters);
                return Ok(page);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get the list of probatory documents.");
                throw;
            }
        }

        /// <summary>
		/// This is for INTERNAL/PRIVATE use only and SHOULD NOT be exposed to the PUBLIC in the API GATEWAY
		/// </summary>
		/// <param name="probatoryDocumentIds">probatoryDocumentIds</param>
		/// <returns></returns>
		[HttpPost("listByIds")]
        public async Task<IActionResult> GetListByProbatoryDocumentIds(int[] probatoryDocumentIds)
        {
            if (probatoryDocumentIds == null) return BadRequest();
            if (probatoryDocumentIds.Length == 0) return BadRequest();

            try
            {
                var result = await _probatoryDocumentService.GetProbatoryDocumentListByIds(probatoryDocumentIds);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get the list of probatory documents.");
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest();
            try
            {
                var result = await _probatoryDocumentService.GetProbatoryDocumentById(id);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the probatory document ID {id}.");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Insert(ProbatoryDocument document)
        {
            if (document == null) return BadRequest();
            try
            {
                await _probatoryDocumentService.InsertProbatoryDocument(document);
                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't create the probatory document.");
                throw;
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProbatoryDocument document)
        {
            if (document == null) return BadRequest();
            try
            {
                await _probatoryDocumentService.UpdateProbatoryDocument(document);
                return Ok(document);
            }
            catch (NonExistingRecordCustomizedException ex)
            {
                _logger.LogWarning(ex, $"Cannot update because the probatory document ID {document.Id} has not been found.");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't update the probatory document ID {document.Id}.");
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

        #endregion
    }

    #region Extension methods

    public static class ProbatoryDocumentControllerPagingAndFilteringExtensions
    {
        public static PagingSettings ToPagingSettings(this ProbatoryDocumentController.PagingAndFiltering pagingAndFiltering)
        {
            return new PagingSettings
            {
                PageNumber = pagingAndFiltering.PageNumber,
                PageSize = pagingAndFiltering.PageSize
            };
        }

        public static ProbatoryDocumentService.Filters ToFilters(this ProbatoryDocumentController.PagingAndFiltering pagingAndFiltering)
        {
            return new ProbatoryDocumentService.Filters
            {
                Keywords = pagingAndFiltering.Keywords.DecodeUTF8().CleanDoubleWhiteSpaces()
            };
        }
    }

    #endregion
}