using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Aide.Core.Cloud.Azure.ServiceBus;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Aide.Claims.Domain.Enumerations;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Services;
using Aide.Claims.WebApi.Adapters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrderClaim = Aide.Claims.Domain.Objects.Claim;

namespace Aide.Claims.WebApi.Controllers
{
    [Authorize]
	[Route("[controller]")]
	[ApiController]
	public class ClaimController : ControllerBase
	{
		private readonly ILogger<ClaimController> _logger;
		private readonly IUserService _userService;
		private readonly IClaimService _claimService;
		private readonly IMessageBrokerAdapter _messageBrokerAdapter;

		public ClaimController(ILogger<ClaimController> logger, IUserService userService, IClaimService claimService, IMessageBrokerAdapter messageBrokerAdapter)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_userService = userService ?? throw new ArgumentNullException(nameof(userService));
			_claimService = claimService ?? throw new ArgumentNullException(nameof(claimService));
			_messageBrokerAdapter = messageBrokerAdapter ?? throw new ArgumentNullException(nameof(messageBrokerAdapter));
		}

		[HttpPost("list")]
		public async Task<IActionResult> GetList(PagingAndFiltering pagingAndFiltering)
		{
			if (pagingAndFiltering == null) return BadRequest();
			if (pagingAndFiltering.PageNumber < 1 || pagingAndFiltering.PageSize < 1) return BadRequest();

			User user;
			try
			{
				var token = Request.GetAuthorizationHeader();
				user = _userService.BuildUserFromJwtToken(token);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Couldn't get the authorization header (jwt token).");
				throw;
			}

			try
			{
				var filters = pagingAndFiltering.ToFilters(user);
				var pagingSettings = pagingAndFiltering.ToPagingSettings();
				var page = await _claimService.GetAllClaims(pagingSettings, filters, EnumViewDetail.Minimum);
				return Ok(page);
			}
			catch (NonExistingRecordCustomizedException)
			{
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Couldn't get the list of claims.");
				throw;
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			if (id <= 0) return BadRequest();

			try
			{
				var result = await _claimService.GetClaimById(id);
				return Ok(result);
			}
			catch (NonExistingRecordCustomizedException)
			{
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't get a Claim by ID {id}.");
				throw;
			}
		}

		[HttpPost]
		public async Task<IActionResult> Insert(OrderClaim claim)
		{
			if (claim == null) return BadRequest();

			try
			{
				await _claimService.InsertClaim(claim);
				return Ok(claim);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Couldn't create a new claim");
				throw;
			}
		}

		[HttpPut]
		public async Task<IActionResult> Update([FromBody] OrderClaim claim)
		{
			if (claim == null) return BadRequest();

			try
			{
				await _claimService.UpdateClaim(claim);
				return Ok(claim);
			}
            catch (ArgumentException)
            {
				return BadRequest();
            }
			catch (NonExistingRecordCustomizedException)
			{
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't update claim ID {claim.Id}");
				throw;
			}
		}

		[HttpPut("{claimId}/status")]
		public async Task<IActionResult> UpdateStatus([FromRoute] int claimId, [FromBody] int statusId)
        {
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			if (statusId <= 0) throw new ArgumentOutOfRangeException(nameof(statusId));

            OrderClaim claim;
            try
            {
				claim = await _claimService.UpdateStatus(claimId, statusId);
            }
			catch (NonExistingRecordCustomizedException)
			{
				return NoContent();
			}
			catch (Exception ex)
            {
				_logger.LogError(ex, $"Couldn't update the status on order {claimId}");
                throw;
            }

            // Verify if the order comes from a different source of AIDE, if the answer is YES then enqueue a notification for the external service.
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Source))
			{
				var aideNotificationMessage = new AideNotificationMessage
				{
					Action = "AideReceiptComplete",
					ClaimId = claim.Id,
					Message = "Aide receipt completed",
					TransactionId = Guid.NewGuid().ToString()
				};
				await _messageBrokerAdapter.SendNotificationMessageToThirdPartySystem(aideNotificationMessage).ConfigureAwait(false);
			}

            return Ok();
        }

		[HttpPost("{claimId}/signature")]
		public async Task<IActionResult> SubmitSignature(int claimId, [FromBody] Signature signature)
		{
			if (claimId <= 0) return BadRequest();
			if (signature == null) return BadRequest();

			_logger.LogInformation($"Signature for claimId {claimId} is about to process.");
			try
			{
				var claimSignature = new ClaimSignature
				{
					ClaimId = claimId,
					Signature = signature.Base64image,
					LocalDate = DateTime.Parse(signature.LocalDate),
					LocalTimeZone = signature.LocalTimeZone
				};

				await _claimService.SignClaim(claimSignature);
				await _messageBrokerAdapter.SendEmailClaimReceiptMessage(claimId);

				return Ok(claimSignature);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"An error happened when trying to process the signature for claimId {claimId}");
				_logger.LogError(JsonConvert.SerializeObject(signature));
				throw;
			}
		}

		[HttpGet("{claimId}/signature")]
		public async Task<IActionResult> GetSignatureByClaimId(int claimId)
		{
			if (claimId <= 0) return BadRequest();
			try
			{
				var signature = await _claimService.GetSignatureByClaimId(claimId);
				if (signature == null) return NoContent();
				var result = new Signature
				{
					Base64image = signature.Signature,
					LocalDate = signature.LocalDate.ToString("u"), // Format to "UniversalSortableDateTimePattern"
					LocalTimeZone = signature.LocalTimeZone,
					DateCreated = signature.DateCreated
				};
				return Ok(result);
			}
			catch (NonExistingRecordCustomizedException)
			{
				return NoContent();
			}
		}

		[HttpPost("removeStaledOrders")]
		public async Task<IActionResult> RemoveStaledOrders([FromBody] double thresholdInHours)
        {
			if (thresholdInHours <= 0) return BadRequest();
            try
            {
				var staledOrdersRemoved = await _claimService.RemoveStaledOrders(thresholdInHours);
				var message = $"Successfully removed {staledOrdersRemoved} orders older than {thresholdInHours} hours.";
				_logger.LogInformation(message);
				return Ok(message);
			}
            catch (Exception ex)
            {
				_logger.LogError(ex, $"An error occurred when removing the stale orders that are older than {thresholdInHours} hours.");
                throw;
            }
		}

		[HttpGet("{claimId}/externalOrderNumber/{externalOrderNumber}/verify")]
		public async Task<IActionResult> ExternalOrderNumberExists([FromRoute] int claimId, string externalOrderNumber)
        {
			// Dev Notes: Be aware that claimId = 0 is valid because it's related to new orders in the front-end that are not saved yet
			if (claimId < 0) return BadRequest();
			if (string.IsNullOrWhiteSpace(externalOrderNumber)) return BadRequest();
			var result = await _claimService.ExternalOrderNumberExists(externalOrderNumber, claimId);
			return Ok(result);
        }

		#region Local classes

		public class PagingAndFiltering
		{
			public int PageSize { get; set; }
			public int PageNumber { get; set; }
			public string Keywords { get; set; }
			public int? StatusId { get; set; }
			public string StoreName { get; set; }
			public int? ServiceTypeId { get; set; }
			public int? InsuranceCompanyId { get; set; }
			public DateTime? StartDate { get; set; }
			public DateTime? EndDate { get; set; }
		}

		#endregion
	}

	#region Extension methods

	public static class ClaimControllerPagingAndFilteringExtensions
	{
		public static PagingSettings ToPagingSettings(this ClaimController.PagingAndFiltering pagingAndFiltering)
		{
			return new PagingSettings
			{
				PageNumber = pagingAndFiltering.PageNumber,
				PageSize = pagingAndFiltering.PageSize
			};
		}

		public static ClaimService.Filters ToFilters(this ClaimController.PagingAndFiltering pagingAndFiltering, User user)
		{
			var defaultInsuranceCompanyId = new List<int>();
			var defaultStoreId = new List<int>();
			if (user.RoleId != EnumUserRoleId.Admin)
			{
				if (user.RoleId == EnumUserRoleId.InsuranceReadOnly)
				{
					defaultInsuranceCompanyId = user.Companies
						.Where(x => x.CompanyTypeId == EnumCompanyTypeId.Insurance)
						.Select(x => x.CompanyId)
						.ToList();
				}
				else if (user.RoleId == EnumUserRoleId.WsAdmin || user.RoleId == EnumUserRoleId.WsOperator)
				{
					defaultStoreId = user.Companies
						.Where(x => x.CompanyTypeId == EnumCompanyTypeId.Store)
						.Select(x => x.CompanyId)
						.ToList();
				}
			}
			return new ClaimService.Filters
			{
				DefaultInsuranceCompanyId = defaultInsuranceCompanyId,
				DefaultStoreId = defaultStoreId,
				Keywords = pagingAndFiltering.Keywords.DecodeUTF8().CleanDoubleWhiteSpaces(),
				StatusId = pagingAndFiltering.StatusId,
				StoreName = pagingAndFiltering.StoreName.DecodeUTF8(),
				ServiceTypeId = pagingAndFiltering.ServiceTypeId,
				InsuranceCompanyId = pagingAndFiltering.InsuranceCompanyId,
				StartDate = pagingAndFiltering.StartDate,
				EndDate = pagingAndFiltering.EndDate
			};
		}
	}

	#endregion
}