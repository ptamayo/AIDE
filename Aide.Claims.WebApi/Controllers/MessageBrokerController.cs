using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aide.Core.Cloud.Azure.ServiceBus;
using Aide.Core.CustomExceptions;
using Aide.Claims.Domain.Enumerations;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Services;
using Aide.Claims.WebApi.Adapters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Aide.Hangfire.Common.Messages;
using Aide.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace Aide.Claims.WebApi.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class MessageBrokerController : ControllerBase
    {
        private readonly ILogger<MessageBrokerController> _logger;
        private readonly IMessageBrokerAdapter _messageBrokerAdapter;

        public MessageBrokerController(ILogger<MessageBrokerController> logger, IMessageBrokerAdapter messageBrokerAdapter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageBrokerAdapter = messageBrokerAdapter ?? throw new ArgumentNullException(nameof(messageBrokerAdapter));
        }

        [HttpPost("emailClaimReceipt")]
        public async Task<IActionResult> SubmitMessage([FromBody] int claimId)
        {
            if (claimId <= 0) return BadRequest();

            try
            {
                // Queue the message
                await _messageBrokerAdapter.SendEmailClaimReceiptMessage(claimId);

                // Return HTTP 200
                return Ok();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost("zipClaimFiles")]
        public async Task<IActionResult> ZipClaimProbatoryDocuments([FromBody] int claimId)
        {
            if (claimId <= 0) return BadRequest();

            try
            {
                // Queue the message
                var defaultZipDocument = await _messageBrokerAdapter.SendZipClaimFilesMessage(claimId);

                // Return In Process Zip Document
                return Ok(defaultZipDocument);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost("pdfExportClaimFiles")]
        public async Task<IActionResult> PdfExportClaimProbatoryDocuments([FromBody] int claimId)
        {
            if (claimId <= 0) return BadRequest();

            try
            {
                // Queue the message
                var defaultZipDocument = await _messageBrokerAdapter.SendPdfExportClaimFilesMessage(claimId);

                // Return In Process Pdf Document
                return Ok(defaultZipDocument);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost("zipAndEmailClaimFiles")]
        public async Task<IActionResult> ZipAndEmailClaimProbatoryDocuments(ZipAndEmailClaimProbatoryDocumentsRequest request)
        {
            if (request == null) return BadRequest();
            if (request.ClaimId <= 0) return BadRequest();
            if (string.IsNullOrWhiteSpace(request.EmailTo)) return BadRequest();

            try
            {
                // Queue the message
                await _messageBrokerAdapter.SendZipAndEmailClaimFilesMessage(request.ClaimId, request.EmailTo);

                // Return In Process Zip Document
                return Ok();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost("dashboard1ClaimsReport")]
        public async Task<IActionResult> Dashboard1ClaimsReport([FromBody] ClaimService.Filters filters)
        {
            if (filters == null) return BadRequest();

            // Get the timezone header from the http post
            string clientTimezone = string.Empty;
            try
            {
                clientTimezone = Request.GetTimezoneHeader();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get the timezone header.");
                throw;
            }

            try
            {
                // Build request base of the filters provided
                var request = new MessageBrokerAdapter.Dashboard1ClaimsReportRequest
                {
                    DefaultInsuranceCompanyId = filters.DefaultInsuranceCompanyId,
                    DefaultStoreId = filters.DefaultStoreId,
                    Keywords = filters.Keywords.DecodeUTF8(),
                    StatusId = filters.StatusId,
                    StoreName = filters.StoreName.DecodeUTF8(),
                    ServiceTypeId = filters.ServiceTypeId,
                    InsuranceCompanyId = filters.InsuranceCompanyId,
                    StartDate = filters.StartDate,
                    EndDate = filters.EndDate,
                    Timezone = clientTimezone
                };
                // Queue the message
                await _messageBrokerAdapter.SendDashboard1ClaimsReportMessage(request);

                // Return HTTP 200
                return Ok();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public class ZipAndEmailClaimProbatoryDocumentsRequest
		{
            public int ClaimId { get; set; }
            public string EmailTo { get; set; }
		}
    }
}