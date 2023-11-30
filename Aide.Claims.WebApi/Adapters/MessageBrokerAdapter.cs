using Aide.Core.Cloud.Azure.ServiceBus;
using Aide.Core.CustomExceptions;
using Aide.Claims.Domain.Enumerations;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Aide.Hangfire.Common.Messages;
using System.Collections.Generic;

namespace Aide.Claims.WebApi.Adapters
{
	public interface IMessageBrokerAdapter
	{
		Task SendEmailClaimReceiptMessage(EmailClaimReceiptMessage message);
		Task SendEmailClaimReceiptMessage(int claimId);
		Task<ClaimDocument> SendZipClaimFilesMessage(int claimId);
		Task<ClaimDocument> SendPdfExportClaimFilesMessage(int claimId);
		Task SendZipAndEmailClaimFilesMessage(int claimId, string emailTo);
		Task SendDashboard1ClaimsReportMessage(MessageBrokerAdapter.Dashboard1ClaimsReportRequest request);
		Task SendZipFilesMessage(ZipFilesMessage message);
        Task SendNotificationMessageToThirdPartySystem(AideNotificationMessage aideNotificationMessage);
    }

	public class MessageBrokerAdapter : IMessageBrokerAdapter
	{
		private readonly IBusService _bus;
		private readonly AppSettings _appSettings;
		private readonly IClaimService _claimService;
		private readonly IClaimDocumentService _claimDocumentService;
		private readonly IDocumentTypeService _documentTypeService;
		private const string EmailClaimReceiptQueue = "plk_email_claim_receipt_queue";
		private const string ZipClaimFilesQueue = "plk_zip_claim_files_queue";
		private const string ZipAndEmailClaimFilesQueue = "plk_zip_and_email_claim_files_queue";
		private const string PdfExportClaimFilesQueue = "plk_pdf_export_claim_files_queue";
		private const string Dashboard1ClaimsReportQueue = "plk_dashboard1_claims_report_queue";
		private const string ZipFilesQueue = "plk_zip_files_queue";
		private const string AideNotificationQueue = "aide_integration_queue";
        private const int TpaDocumentsGroupId = 4;

		public MessageBrokerAdapter(IBusService bus, AppSettings appSettings, IClaimService claimService, IClaimDocumentService claimDocumentService, IDocumentTypeService documentTypeService)
		{
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_claimService = claimService ?? throw new ArgumentNullException(nameof(claimService));
			_claimDocumentService = claimDocumentService ?? throw new ArgumentNullException(nameof(claimDocumentService));
			_documentTypeService = documentTypeService ?? throw new ArgumentNullException(nameof(documentTypeService));
		}

		public async Task SendEmailClaimReceiptMessage(int claimId)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));

			var claim = await _claimService.GetClaimById(claimId).ConfigureAwait(false);

			string[] claimProbatoryDocuments = null;
			int claimProbatoryDocumentId = 0;
			if (claim.ClaimProbatoryDocuments.Any())
			{
				var query = from cpd in claim.ClaimProbatoryDocuments
							where cpd.GroupId != TpaDocumentsGroupId // Notice that TPA Docs are skipped here
								&& cpd.ProbatoryDocumentId != _appSettings.ReceiptDocumentConfig.ProbatoryDocumentId // Notice the signed receipt is skipped too
							orderby cpd.GroupId, cpd.ClaimItemId, cpd.SortPriority
							group cpd by cpd.ProbatoryDocument.Name into gx
							select new
							{
								ProbatoryDocumentName = gx.Key,
								Count = gx.Count()
							};
				claimProbatoryDocuments = query.Select(q => q.Count > 1 ? $"{q.Count} x {q.ProbatoryDocumentName}" : q.ProbatoryDocumentName).ToArray();
				// IMPORTANT: Below it's getting the claim_probatory_document_id where the receipt will be attached
				claimProbatoryDocumentId = claim.ClaimProbatoryDocuments.Where(x => x.ProbatoryDocumentId == _appSettings.ReceiptDocumentConfig.ProbatoryDocumentId).FirstOrDefault().Id;
			}

			var message = new EmailClaimReceiptMessage
			{
				ClaimId = claimId,
				ExternalOrderNumber = claim.ExternalOrderNumber,
				CustomerFullName = claim.CustomerFullName,
				InsuranceCompanyName = claim.InsuranceCompany.Name,
				StoreName = claim.Store.Name,
				StoreEmail = claim.Store.Email,
				ClaimProbatoryDocuments = claimProbatoryDocuments,
				ClaimProbatoryDocumentId = claimProbatoryDocumentId
			};

			await SendEmailClaimReceiptMessage(message).ConfigureAwait(false);
		}

		public async Task SendEmailClaimReceiptMessage(EmailClaimReceiptMessage message)
		{
			if (message == null) throw new ArgumentNullException(nameof(message));

			//// Delete previous document if any
			//try
			//{
			//	await _claimDocumentService.DeleteClaimDocumentByClaimIdAndDocumentTypeId(message.ClaimId, _appSettings.ReceiptDocumentConfig.DocumentTypeId).ConfigureAwait(false);
			//}
			//catch (NonExistingRecordCustomizedException) { }

			//// Create new document which default status is "InProcess"
			//var documentType = await _documentTypeService.GetDocumentTypeById(_appSettings.ReceiptDocumentConfig.DocumentTypeId).ConfigureAwait(false);
			//var defaultReceiptDocument = new ClaimDocument
			//{
			//	ClaimId = message.ClaimId,
			//	DocumentTypeId = message.ClaimDocumentTypeId,
			//	DocumentType = documentType,
			//	GroupId = message.ClaimDocumentGroupId,
			//	SortPriority = message.ClaimDocumentSortPriority,
			//	StatusId = EnumClaimDocumentStatusId.InProcess
			//};
			//await _claimDocumentService.InsertClaimDocument(defaultReceiptDocument).ConfigureAwait(false);

			// Send message to hangfire
			var queueUrl = _appSettings.ServiceBusConfig.Queue[EmailClaimReceiptQueue];
			var endpoint = await _bus.GetSendEndpoint(queueUrl).ConfigureAwait(false);
			await endpoint.Send<EmailClaimReceiptMessage>(message).ConfigureAwait(false);

			//return defaultReceiptDocument;
		}

		public async Task<ClaimDocument> SendZipClaimFilesMessage(int claimId)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));

			// Delete previous document if any
			try
			{
				await _claimDocumentService.DeleteClaimDocumentByClaimIdAndDocumentTypeId(claimId, _appSettings.ZipDocumentConfig.DocumentTypeId).ConfigureAwait(false);
			}
			catch (NonExistingRecordCustomizedException) { }

			// Create new document which default status is "InProcess"
			var documentType = await _documentTypeService.GetDocumentTypeById(_appSettings.ZipDocumentConfig.DocumentTypeId).ConfigureAwait(false);
			var defaultZipDocument = new ClaimDocument
			{
				ClaimId = claimId,
				DocumentTypeId = _appSettings.ZipDocumentConfig.DocumentTypeId,
				DocumentType = documentType,
				GroupId = _appSettings.ZipDocumentConfig.GroupId,
				SortPriority = _appSettings.ZipDocumentConfig.SortPriority,
				StatusId = EnumClaimDocumentStatusId.InProcess
			};
			await _claimDocumentService.InsertClaimDocument(defaultZipDocument).ConfigureAwait(false);

			// Send message to hangfire
			var message = new ZipClaimFilesMessage
			{
				ClaimId = claimId,
				ClaimDocumentTypeId = _appSettings.ZipDocumentConfig.DocumentTypeId,
				ClaimDocumentGroupId = _appSettings.ZipDocumentConfig.GroupId,
				ClaimDocumentSortPriority = _appSettings.ZipDocumentConfig.SortPriority
			};
			var queueUrl = _appSettings.ServiceBusConfig.Queue[ZipClaimFilesQueue];
			var endpoint = await _bus.GetSendEndpoint(queueUrl).ConfigureAwait(false);
			await endpoint.Send<ZipClaimFilesMessage>(message).ConfigureAwait(false);

			return defaultZipDocument;
		}

		public async Task<ClaimDocument> SendPdfExportClaimFilesMessage(int claimId)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));

			// Delete previous document if any
			try
			{
				await _claimDocumentService.DeleteClaimDocumentByClaimIdAndDocumentTypeId(claimId, _appSettings.PdfDocumentConfig.DocumentTypeId).ConfigureAwait(false);
			}
			catch (NonExistingRecordCustomizedException) { }

			// Create new document which default status is "InProcess"
			var documentType = await _documentTypeService.GetDocumentTypeById(_appSettings.PdfDocumentConfig.DocumentTypeId).ConfigureAwait(false);
			var defaultPdfDocument = new ClaimDocument
			{
				ClaimId = claimId,
				DocumentTypeId = _appSettings.PdfDocumentConfig.DocumentTypeId,
				DocumentType = documentType,
				GroupId = _appSettings.PdfDocumentConfig.GroupId,
				SortPriority = _appSettings.PdfDocumentConfig.SortPriority,
				StatusId = EnumClaimDocumentStatusId.InProcess
			};
			await _claimDocumentService.InsertClaimDocument(defaultPdfDocument).ConfigureAwait(false);

			// Send message to hangfire
			var message = new PdfExportClaimFilesMessage
			{
				ClaimId = claimId,
				ClaimDocumentTypeId = _appSettings.PdfDocumentConfig.DocumentTypeId,
				ClaimDocumentGroupId = _appSettings.PdfDocumentConfig.GroupId,
				ClaimDocumentSortPriority = _appSettings.PdfDocumentConfig.SortPriority
			};
			var queueUrl = _appSettings.ServiceBusConfig.Queue[PdfExportClaimFilesQueue];
			var endpoint = await _bus.GetSendEndpoint(queueUrl).ConfigureAwait(false);
			await endpoint.Send<PdfExportClaimFilesMessage>(message).ConfigureAwait(false);

			return defaultPdfDocument;
		}

		public async Task SendZipAndEmailClaimFilesMessage(int claimId, string emailTo)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));
			if (string.IsNullOrWhiteSpace(emailTo)) throw new ArgumentNullException(nameof(emailTo));

			// Send message to hangfire
			var message = new ZipAndEmailClaimFilesMessage
			{
				ClaimId = claimId,
				EmailTo = emailTo
			};
			var queueUrl = _appSettings.ServiceBusConfig.Queue[ZipAndEmailClaimFilesQueue];
			var endpoint = await _bus.GetSendEndpoint(queueUrl).ConfigureAwait(false);
			await endpoint.Send<ZipAndEmailClaimFilesMessage>(message).ConfigureAwait(false);
		}

		public async Task SendDashboard1ClaimsReportMessage(Dashboard1ClaimsReportRequest request)
        {
			if (request == null) throw new ArgumentNullException(nameof(request));
			var message = new Dashboard1ClaimsReportMessage
			{
				DefaultInsuranceCompanyId = request.DefaultInsuranceCompanyId,
				DefaultStoreId = request.DefaultStoreId,
				Keywords = request.Keywords,
				StatusId = request.StatusId,
				StoreName = request.StoreName,
				ServiceTypeId = request.ServiceTypeId,
				InsuranceCompanyId = request.InsuranceCompanyId,
				StartDate = request.StartDate,
				EndDate = request.EndDate,
				Timezone = request.Timezone
			};
			var queueUrl = _appSettings.ServiceBusConfig.Queue[Dashboard1ClaimsReportQueue];
			var endpoint = await _bus.GetSendEndpoint(queueUrl).ConfigureAwait(false);
			await endpoint.Send<Dashboard1ClaimsReportMessage>(message).ConfigureAwait(false);
		}

		public async Task SendZipFilesMessage(ZipFilesMessage message)
		{
            if (message == null) throw new ArgumentNullException(nameof(message));
            var queueUrl = _appSettings.ServiceBusConfig.Queue[ZipFilesQueue];
            var endpoint = await _bus.GetSendEndpoint(queueUrl).ConfigureAwait(false);
            await endpoint.Send<ZipFilesMessage>(message).ConfigureAwait(false);
        }

        public async Task SendNotificationMessageToThirdPartySystem(AideNotificationMessage aideNotificationMessage)
        {
            if (aideNotificationMessage == null) throw new ArgumentNullException(nameof(aideNotificationMessage));
            var queueUrl = _appSettings.ServiceBusConfig.Queue[AideNotificationQueue];
            var endpoint = await _bus.GetSendEndpoint(queueUrl).ConfigureAwait(false);
            await endpoint.Send<AideNotificationMessage>(aideNotificationMessage).ConfigureAwait(false);
        }

        #region Local classes

        public class Dashboard1ClaimsReportRequest
		{
			public IEnumerable<int> DefaultInsuranceCompanyId { get; set; }
			public IEnumerable<int> DefaultStoreId { get; set; }
			public string Keywords { get; set; }
			public int? StatusId { get; set; }
			public string StoreName { get; set; }
			public int? ServiceTypeId { get; set; }
			public int? InsuranceCompanyId { get; set; }
			public DateTime? StartDate { get; set; }
			public DateTime? EndDate { get; set; }
			public string Timezone { get; set; }
		}

		#endregion

		
	}
}
