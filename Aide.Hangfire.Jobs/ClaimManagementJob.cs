using Aide.Core.Cloud.Azure.SendGrid;
using Aide.Core.CustomExceptions;
using Aide.Hangfire.Common.Messages;
using Aide.Hangfire.Jobs.Settings;
using Aide.Hangfire.Services;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs
{
	public class ClaimManagementJob
	{
		private readonly ILogger<ClaimManagementJob> _logger;
		private readonly IClaimService _claimService;
		private readonly ClaimManagementJobConfig _configSettings;

		public ClaimManagementJob(ILogger<ClaimManagementJob> logger, IClaimService claimService, ClaimManagementJobConfig configSettings)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_claimService = claimService ?? throw new ArgumentNullException(nameof(claimService));
			_configSettings = configSettings ?? throw new ArgumentNullException(nameof(configSettings));
		}

		public async Task UpdateClaimStatus(UpdateClaimStatusMessage message)
		{
			#region Mock data
			//message = new UpdateClaimStatusMessage
			//{
			//	ClaimId = 32798,
			//	ClaimStatusId = 10
			//};
			#endregion

			if (message == null)
			{
				_logger.LogError("Message cannot be null.");
				return;
			}

			if (message.ClaimId <= 0)
			{
				_logger.LogError("Invalid claim id provided.");
				return;
			}

			if (message.ClaimStatusId <= 0)
			{
				_logger.LogError("Invalid claim status id provided.");
				return;
			}

			try
			{
				await _claimService.UpdateStatus(message.ClaimId, message.ClaimStatusId).ConfigureAwait(false);
			}			
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't update the claim id {message.ClaimId} to status id {message.ClaimStatusId} due to an unhandled error.");
				throw;
			}
		}

		public async Task UpdateClaim(UpdateClaimMessage message)
		{
			#region Mock data
			//message = new UpsertClaimMessage
			//{
			//	ClaimUpsertRequest = new ClaimService.ClaimServiceRequest
			//{
			//		Id = 32798,
			//		ClaimStatusId = Domain.Enumerations.EnumClaimStatusId.Completed,
			//		StoreId = 142,
			//		ClaimNumber = "AB12345C/22",
			//		ClaimTypeId = Domain.Enumerations.EnumClaimTypeId.OrdenDeServicio,
			//		CreatedByUserId = 1,
			//		CustomerFullName = "Santiago Sanudo Sepulveda",
			//		InsuranceCompanyId = 4,
			//		ItemsQuantity = 2,
			//		PolicyNumber = "1234",
			//		PolicySubsection = "A",
			//		ReportNumber = "1234",
			//		ClaimProbatoryDocumentStatusId = Domain.Enumerations.EnumClaimProbatoryDocumentStatusId.InProgress,
			//		ExternalOrderNumber = "AXA220600009",
			//		HasDepositSlip = true,
			//		IsDepositSlipRequired = true,
			//		DateCreated = Convert.ToDateTime("2022-06-15 05:02:49"),
			//		DateModified = DateTime.UtcNow
			//	}
			//};
			#endregion

			if (message == null)
			{
				_logger.LogError("Message cannot be null.");
				return;
			}

			if (message.ClaimUpdateRequest == null)
			{
				_logger.LogError("Claim cannot be null.");
				return;
			}

			if (message.ClaimUpdateRequest.Id <= 0)
			{
				_logger.LogError("Invalid claim id provided.");
				return;
			}

			try
			{
				await _claimService.UpdateClaim(message.ClaimUpdateRequest).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't update the claim id {message.ClaimUpdateRequest.Id} due to an unhandled error.");
				throw;
			}
		}

		public async Task InsertClaim(InsertClaimMessage message)
		{
            #region Mock data
            //message = new UpsertClaimMessage
            //{
            //    ClaimUpsertRequest = new ClaimService.ClaimServiceRequest
            //    {
            //        ClaimStatusId = Domain.Enumerations.EnumClaimStatusId.InProgress,
            //        StoreId = 142,
            //        ClaimNumber = "AB12345C/22",
            //        ClaimTypeId = Domain.Enumerations.EnumClaimTypeId.OrdenDeServicio,
            //        CreatedByUserId = 1,
            //        CustomerFullName = "Santiago Sanudo Sepulveda",
            //        InsuranceCompanyId = 4,
            //        ItemsQuantity = 2,
            //        PolicyNumber = "1234",
            //        PolicySubsection = "A",
            //        ReportNumber = "1234",
            //        ClaimProbatoryDocumentStatusId = Domain.Enumerations.EnumClaimProbatoryDocumentStatusId.InProgress,
            //        ExternalOrderNumber = "AXA220600009",
            //        HasDepositSlip = true,
            //        IsDepositSlipRequired = true,
            //        DateCreated = DateTime.UtcNow,
            //        DateModified = DateTime.UtcNow
            //    }
            //};
            #endregion

            if (message == null)
			{
				_logger.LogError("Message cannot be null.");
				return;
			}

			if (message.ClaimInsertRequest == null)
			{
				_logger.LogError("Claim cannot be null.");
				return;
			}

			try
			{
				await _claimService.InsertClaim(message.ClaimInsertRequest).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't insert the claim id {message.ClaimInsertRequest.Id} due to an unhandled error.");
				throw;
			}
		}

		#region Local Classes

		public class ClaimManagementJobConfig
		{
			public string UrlWeb { get; set; }
            public bool IsEmailServiceEnabled { get; set; }
            public string EmailFrom { get; set; }
			public string EmailForSupport { get; set; }
			public SendGridConfig SendGridConfig { get; set; }
		}

		#endregion
	}
}
