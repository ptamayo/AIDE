using Aide.Core.CustomExceptions;
using Aide.Core.Domain;
using Aide.Core.Extensions;
using Aide.Core.Interfaces;
using Aide.Core.Media.MagickDotNet;
using Aide.Hangfire.Common.Messages;
using Aide.Hangfire.Domain.Enumerations;
using Aide.Hangfire.Domain.Objects;
using Aide.Hangfire.Domain.SignalRMessages;
using Aide.Hangfire.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs
{
    public class ClaimProbatoryDocumentsJob
	{
		private readonly ILogger<ClaimProbatoryDocumentsJob> _logger;
		private readonly ConfigSettings _configSettings;
		private readonly IClaimService _claimService;
		private readonly IClaimDocumentService _claimDocumentService;
		private readonly IInsuranceCollageService _insuranceCollageService;
		private readonly ICollageAdapter _collageAdapter;
		private readonly IZipArchiveAdapter _zipArchiveAdapter;
		private readonly IFileSystemAdapter _fsa;
		private readonly IInsuranceProbatoryDocumentService _insuranceProbatoryDocumentService;
		private readonly INotificationService _notificationService;

		//private const int ElectronicInvoiceGroup = 1;
		private const int ClaimReceiptGroup = 3;
		private const int ClaimAdminDocsGroup = 1;
		private const int ClaimPicturesGroup = 2;
		private const int ClaimPicturesXItemsGroup = 3;
		private const int ClaimTpaDocuments = 4;
		private const string DefaultZipMimeType = "application/zip";
		private const string DefaultPdfMimeType = "application/pdf";
		private const string DefaultJsonMimeType = "application/json";
		private const string CollageMimeType = "image/jpeg";
		private const string DefaulZipMessageType = "ZipClaimFilesReady";
		private const string DefaulPdfMessageType = "PdfClaimFilesReady";
		private const string htmlNewLine = "<br/>";

		public ClaimProbatoryDocumentsJob(
			ILogger<ClaimProbatoryDocumentsJob> logger,
			ConfigSettings configSettings,
			IClaimService claimService,
			IClaimDocumentService claimDocumentService,
			IInsuranceCollageService insuranceCollageService,
			ICollageAdapter collageAdapter,
			IZipArchiveAdapter zipArchiveAdapter,
			IFileSystemAdapter fsa,
			IInsuranceProbatoryDocumentService insuranceProbatoryDocumentService,
			INotificationService notificationService)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_configSettings = configSettings ?? throw new ArgumentNullException(nameof(configSettings));
			_claimService = claimService ?? throw new ArgumentNullException(nameof(claimService));
			_claimDocumentService = claimDocumentService ?? throw new ArgumentNullException(nameof(claimDocumentService));
			_insuranceCollageService = insuranceCollageService ?? throw new ArgumentNullException(nameof(insuranceCollageService));
			_collageAdapter = collageAdapter ?? throw new ArgumentNullException(nameof(collageAdapter));
			_zipArchiveAdapter = zipArchiveAdapter ?? throw new ArgumentNullException(nameof(zipArchiveAdapter));
			_fsa = fsa ?? throw new ArgumentNullException(nameof(fsa));
			_insuranceProbatoryDocumentService = insuranceProbatoryDocumentService ?? throw new ArgumentNullException(nameof(insuranceProbatoryDocumentService));
			_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
		}

		public async Task ZipClaimProbatoryDocuments(ZipClaimFilesMessage arg)
		{
			using (_logger.BeginScope(new Dictionary<string, object>
			{
				["ClaimId"] = arg.ClaimId
			}))
			{
				_logger.LogInformation($"Zipping probatory documents for ClaimId {arg.ClaimId}");

				// Prepare the temporary path to save
				var pathToSaveTemp = Path.Combine(_configSettings.TemporaryFilesConfig.PathToSave, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
				if (!_fsa.DirectoryExists(pathToSaveTemp)) _fsa.CreateDirectoryRecursively(pathToSaveTemp);
				var baseUrlTemp = $"{_configSettings.TemporaryFilesConfig.BaseUrl}{DateTime.UtcNow.ToString(@"/yyyy/MM")}";

				// #############
				// 1. Load claim
				// #############

				var claim = await _claimService.GetClaimById(arg.ClaimId).ConfigureAwait(false);

				// ######################################################################################
				// 2. Verify all documents exist: Electronic Invoice (XML and PDF), Admin. Docs, Pictures
				// ######################################################################################

				// Receipt
				var receiptCompleted = true;
				if (claim.ClaimDocuments.Count(x => x.GroupId == ClaimReceiptGroup && x.StatusId != EnumClaimDocumentStatusId.Completed) == 0)
				{
					_logger.LogInformation("General Documents completed");
				}
				else
				{
					_logger.LogWarning("General Documents ARE NOT completed");
					receiptCompleted = false;
				}
				// (4) TPA Docs
				var tpaDocsCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimTpaDocuments && x.Media == null) == 0)
				{
					_logger.LogInformation("TPA Docs completed");
				}
				else
				{
					_logger.LogWarning("TPA Docs ARE NOT completed");
					tpaDocsCompleted = false;
				}
				// (1) Admin Docs
				var adminDocsCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimAdminDocsGroup && x.Media == null) == 0)
				{
					_logger.LogInformation("Admin Docs completed");
				}
				else
				{
					_logger.LogWarning("Admin Docs ARE NOT completed");
					adminDocsCompleted = false;
				}
				// (2) Pictures
				var picturesCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimPicturesGroup && x.Media == null) == 0)
				{
					_logger.LogInformation("Pictures completed");
				}
				else
				{
					_logger.LogWarning("Pictures ARE NOT completed");
					picturesCompleted = false;
				}
				// (3) Pictures x Item
				var picturesXItemsCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimPicturesXItemsGroup && x.Media == null) == 0)
				{
					_logger.LogInformation("Pictures x Item(s) completed");
				}
				else
				{
					_logger.LogWarning("Pictures x Item(s) ARE NOT completed");
					picturesXItemsCompleted = false;
				}

				// #####################################
				// 3. Verify the customer has signed off
				// #####################################

				var signatureCompleted = true;
				var signature = await _claimService.GetSignatureByClaimId(arg.ClaimId).ConfigureAwait(false);
				if (signature != null)
				{
					_logger.LogInformation("Signature completed");
				}
				else
				{
					_logger.LogWarning("Signature IS NOT completed");
					signatureCompleted = false;
				}

                // ###########################
                // Build collage if applicable
                // ###########################

                var buildCollageResult = await BuildCollage(claim, pathToSaveTemp, baseUrlTemp).ConfigureAwait(false);

                // #################
                // 4. Build Zip File
                // #################

                if (!receiptCompleted || !tpaDocsCompleted || !adminDocsCompleted || !picturesCompleted || !picturesXItemsCompleted || !signatureCompleted)
				{
					_logger.LogWarning("Cannot zip the files due missing documents or signature");
					return;
				}

				// Pull the exporting document settings
				InsuranceExportSettings exportSettings;
				try
				{
					exportSettings = await _insuranceProbatoryDocumentService.GetInsuranceExportSettings(claim.InsuranceCompanyId, claim.ClaimTypeId, EnumExportTypeId.ZIP).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					_logger.LogError($"Cannot pull the exporting settings for insurance company ID {claim.InsuranceCompanyId} and claim type ID {claim.ClaimTypeId} and export type ID {EnumExportTypeId.PDF}", ex);
					throw;
				}

				// Probatory documents
				var exportProbatoryDocuments = from x in exportSettings.ExportDocuments
											   join c in claim.ClaimProbatoryDocuments on x.ProbatoryDocumentId equals c.ProbatoryDocumentId
											   where x.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument 
													&& c.Media != null // Just in case document where removed from the configuration after the order was created and the document was not uploaded
											   select new HostedFile
											   {
												   SortPriority = x.SortPriority,
												   Url = c.Media.Url,
												   Filename = c.Media.Filename,
												   DocumentName = c.ProbatoryDocument.Name,
												   DocumentOrientation = (int)c.ProbatoryDocument.Orientation
											   };

				// Collage(s)
				var exportCollages = from x in exportSettings.ExportDocuments
									 join c in buildCollageResult.Collages on x.CollageId equals c.Id
									 where x.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage
											&& c.Media != null // Here makes sure the collage file was created for real 
									 select new HostedFile
									 {
										 SortPriority = x.SortPriority,
										 Url = c.Media.Url,
										 Filename = c.Media.Filename,
										 DocumentName = c.Name,
										 DocumentOrientation = (int)EnumDocumentOrientationId.NA // NA = Not Applicable (this will prevent fix orientation/rotation)
									 };

				// Resize all files (except the electronic invoice files and collage because it's already in the size that is needed)
				if (exportProbatoryDocuments.Any())
                {
					exportProbatoryDocuments = _collageAdapter.ResizeMediaFiles(exportProbatoryDocuments, pathToSaveTemp, baseUrlTemp, _configSettings.MediaEngineConfig.ResizeImageWidth).ToList();
				}
				if (exportCollages.Any())
                {
					exportCollages = _collageAdapter.ResizeMediaFiles(exportCollages, pathToSaveTemp, baseUrlTemp, _configSettings.MediaEngineConfig.ResizeImageWidth).ToList();
				}

				var files = new List<HostedFile>();
				if (exportProbatoryDocuments.Any())
				{
					files.AddRange(exportProbatoryDocuments);
				}
				if (exportCollages.Any())
				{
					files.AddRange(exportCollages);
				}

				var zipFileCreated = true;
				var newClaimDocument = new Document();
				try
				{
					var pathToSaveZip = Path.Combine(_configSettings.ZipFilesConfig.PathToSave, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
					if (!_fsa.DirectoryExists(pathToSaveZip)) _fsa.CreateDirectoryRecursively(pathToSaveZip);
					var fileExtension = ".zip";
					var fileName = $"{claim.ExternalOrderNumber.CleanAllSpecialChars().Trim()}_{DateTime.UtcNow.Ticks}{fileExtension}";
					//var fileName = claim.ExternalOrderNumber.Trim() + fileExtension;
					var newFileName = _fsa.GenerateUniqueFileName(pathToSaveZip, fileName);
					var fullPath = Path.Combine(pathToSaveZip, newFileName);
					_zipArchiveAdapter.ZipHostedFileList(files, fullPath);
					_logger.LogInformation("Zip file successfully created");

					newClaimDocument.Filename = fullPath;
					newClaimDocument.MimeType = DefaultZipMimeType;
					newClaimDocument.Url = $"{_configSettings.ZipFilesConfig.BaseUrl}{DateTime.UtcNow.ToString(@"/yyyy/MM/")}{newFileName}";
				}
				catch (Exception ex)
				{
					zipFileCreated = false;
					_logger.LogError(ex, "An error happened in the process of creating the ZIP file");
				}

				// ###################################
				// 5. Attach the Zip File in the Claim (add claim document / document type: zip (id: 102) / group id: 2).
				// ###################################

				var documentAttached = true;
				ClaimDocument claimDocument = null;
				if (zipFileCreated)
				{
					try
					{
						claimDocument = await _claimDocumentService.AttachDocument(
							claimId: arg.ClaimId,
							document: newClaimDocument,
							documentTypeId: arg.ClaimDocumentTypeId,
							overwrite: true,
							sortPriority: arg.ClaimDocumentSortPriority,
							groupId: arg.ClaimDocumentGroupId)
							.ConfigureAwait(false);
						_logger.LogInformation("Zip file successfully attached to the claim order");
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "An error happened when attached the ZIP file in the claim order");
						documentAttached = false;
					}
				}

				// ##################################
				// 6. SignalR and Email notifications
				// ##################################

				if (zipFileCreated && documentAttached)
				{
					// SignalR
					var message = new MessageZipClaimFilesReady
					{
						Title = "Archivo Zip Generado",
						Content = @$"Taller: {claim.Store.Name}{htmlNewLine}AGRI No.: {claim.ExternalOrderNumber}",
						Url = newClaimDocument.Url,
						HasUrl = true,
						ClaimId = claim.Id,
						DocumentId = claimDocument.DocumentId,
						HasClaim = true
					};
					var notification = new Notification
					{
						Source = nameof(ClaimProbatoryDocumentsJob),
						Target = EnumUserRoleId.Admin.ToString(),
						Type = EnumNotificationTypeId.GroupMessage,
						MessageType = DefaulZipMessageType,
						Message = JsonConvert.SerializeObject(message),
					};
					try
					{
						var notificationResult = await _notificationService.SendNotification(notification).ConfigureAwait(false);
						_logger.LogInformation("The notification to SignalR was successful", notificationResult);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "The notification to SignalR failed");
						throw;
					}
					// Email
					// ...
				}

				// ###
				// END
				// ###
			}
		}

		public async Task ZipClaimProbatoryDocumentsForStoreOnly(ZipAndEmailClaimFilesMessage arg)
		{
			using (_logger.BeginScope(new Dictionary<string, object>
			{
				["ClaimId"] = arg.ClaimId
			}))
			{
				_logger.LogInformation($"Zipping probatory documents for ClaimId {arg.ClaimId}");

				// Prepare the temporary path to save
				var pathToSaveTemp = Path.Combine(_configSettings.TemporaryFilesConfig.PathToSave, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
				if (!_fsa.DirectoryExists(pathToSaveTemp)) _fsa.CreateDirectoryRecursively(pathToSaveTemp);
				var baseUrlTemp = $"{_configSettings.TemporaryFilesConfig.BaseUrl}{DateTime.UtcNow.ToString(@"/yyyy/MM")}";

				// #############
				// 1. Load claim
				// #############

				var claim = await _claimService.GetClaimById(arg.ClaimId).ConfigureAwait(false);

				// ######################################################################################
				// 2. Verify all documents exist: Electronic Invoice (XML and PDF), Admin. Docs, Pictures
				// ######################################################################################

				// Receipt
				var receiptCompleted = true;
				if (claim.ClaimDocuments.Count(x => x.GroupId == ClaimReceiptGroup && x.StatusId != EnumClaimDocumentStatusId.Completed) == 0)
				{
					_logger.LogInformation("Receipt completed");
				}
				else
				{
					_logger.LogWarning("Receipt IS NOT completed");
					receiptCompleted = false;
				}
				// (1) Admin Docs
				var adminDocsCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimAdminDocsGroup && x.Media == null) == 0)
				{
					_logger.LogInformation("Admin Docs completed");
				}
				else
				{
					_logger.LogWarning("Admin Docs ARE NOT completed");
					adminDocsCompleted = false;
				}
				// (2) Pictures
				var picturesCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimPicturesGroup && x.Media == null) == 0)
				{
					_logger.LogInformation("Pictures completed");
				}
				else
				{
					_logger.LogWarning("Pictures ARE NOT completed");
					picturesCompleted = false;
				}
				// (3) Pictures x Item
				var picturesXItemsCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimPicturesXItemsGroup && x.Media == null) == 0)
				{
					_logger.LogInformation("Pictures x Item(s) completed");
				}
				else
				{
					_logger.LogWarning("Pictures x Item(s) ARE NOT completed");
					picturesXItemsCompleted = false;
				}

				// #####################################
				// 3. Verify the customer has signed off
				// #####################################
				
				var signatureCompleted = true;
				var signature = await _claimService.GetSignatureByClaimId(arg.ClaimId).ConfigureAwait(false);
				if (signature != null)
				{
					_logger.LogInformation("Signature completed");
				}
				else
				{
					_logger.LogWarning("Signature IS NOT completed");
					signatureCompleted = false;
				}

				// ###########################
				// Build collage if applicable
				// ###########################

				var buildCollageResult = await BuildCollage(claim, pathToSaveTemp, baseUrlTemp).ConfigureAwait(false);

				// #################
				// 4. Build Zip File
				// #################

				if (!receiptCompleted || !adminDocsCompleted || !picturesCompleted || !picturesXItemsCompleted || !signatureCompleted)
				{
					_logger.LogWarning("Cannot zip the files due missing documents or signature");
					return;
				}

				// Admin docs + Photos + Photos x Item
				var probatoryDocuments = claim.ClaimProbatoryDocuments.Where(x => !buildCollageResult.ClaimProbatoryDocumentsAddedToCollages.Any(d => d.ProbatoryDocumentId == x.ProbatoryDocumentId)
						&& x.GroupId != ClaimTpaDocuments // Ignore the TPA Documents (if any) i.e. PDF and XML Invoice
						&& x.Media != null) // Just in case document where removed from the configuration after the order was created and the document was not uploaded
					.Select(x => new HostedFile
					{
						Url = x.Media.Url,
						Filename = x.Media.Filename,
						DocumentName = x.ProbatoryDocument.Name
					})
					.ToList();
				var collageDocuments = buildCollageResult.Collages.Where(x => x.Media != null) // Here makes sure the collage file was created for real
					.Select(x => new HostedFile
					{
						Url = x.Media.Url,
						Filename = x.Media.Filename,
						DocumentName = x.Name
					})
					.ToList();

				// Resize all files (images only)
				probatoryDocuments = _collageAdapter.ResizeMediaFiles(probatoryDocuments, pathToSaveTemp, baseUrlTemp, _configSettings.MediaEngineConfig.ResizeImageWidth).ToList();

				var files = new List<HostedFile>();
				files.AddRange(probatoryDocuments);
				files.AddRange(collageDocuments);

				var zipFileCreated = true;
				var newClaimDocument = new Document();
				try
				{
					var pathToSave = pathToSaveTemp;
					if (!_fsa.DirectoryExists(pathToSave)) _fsa.CreateDirectoryRecursively(pathToSave);
					var fileExtension = ".zip";
					var fileName = $"{claim.ExternalOrderNumber.CleanAllSpecialChars().Trim()}_{DateTime.UtcNow.Ticks}{fileExtension}";
					var newFileName = _fsa.GenerateUniqueFileName(pathToSave, fileName);
					var fullPath = Path.Combine(pathToSave, newFileName);
					_zipArchiveAdapter.ZipHostedFileList(files, fullPath);
					_logger.LogInformation("Zip file successfully created");

					newClaimDocument.Filename = fullPath;
					newClaimDocument.MimeType = DefaultZipMimeType;
					newClaimDocument.Url = $"{baseUrlTemp}/{newFileName}";
				}
				catch (Exception ex)
				{
					zipFileCreated = false;
					_logger.LogError(ex, "An error happened in the process of creating the ZIP file");
				}

				// ##################################
				// 5. SignalR and Email notifications
				// ##################################

				if (zipFileCreated) //  && documentAttached
				{
					// SignalR
					var message = new MessageZipClaimFilesReady
					{
						Title = "Archivo Zip Generado (temp)",
						Content = @$"Taller: {claim.Store.Name}{htmlNewLine}AGRI No.: {claim.ExternalOrderNumber}",
						Url = newClaimDocument.Url,
						HasUrl = true,
						ClaimId = claim.Id,
						HasClaim = true
					};
					var notification = new Notification
					{
						Source = nameof(ClaimProbatoryDocumentsJob),
						Target = arg.EmailTo,
						Type = EnumNotificationTypeId.PrivateMessage,
						MessageType = DefaulZipMessageType,
						Message = JsonConvert.SerializeObject(message),
                    };
					try
					{
						var notificationResult = await _notificationService.SendNotification(notification).ConfigureAwait(false);
						_logger.LogInformation("The notification to SignalR was successful", notificationResult);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "The notification to SignalR failed");
						throw;
					}
					// Email
					// ...
				}

				// ###
				// END
				// ###
			}
		}

		public async Task ExportPDF(PdfExportClaimFilesMessage arg)
		{
			using (_logger.BeginScope(new Dictionary<string, object>
			{
				["ClaimId"] = arg.ClaimId
			}))
			{
				_logger.LogInformation($"Exporting probatory documents to single PDF for ClaimId {arg.ClaimId}");

				// #############
				// 1. Load claim
				// #############

				var claim = await _claimService.GetClaimById(arg.ClaimId).ConfigureAwait(false);

                // ######################################################################################
                // 2. Verify all documents exist: Electronic Invoice (XML and PDF), Admin. Docs, Pictures
                // ######################################################################################

                // General Docs i.e. Receipt
                var generalDocsCompleted = true;
                if (claim.ClaimDocuments.Count(x => x.GroupId == ClaimReceiptGroup && x.StatusId != EnumClaimDocumentStatusId.Completed) == 0)
                {
                    _logger.LogInformation("General Documents completed");
                }
                else
                {
                    _logger.LogWarning("General Documents ARE NOT completed");
					generalDocsCompleted = false;
                }
                // (4) TPA Docs
                var tpaDocsCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimTpaDocuments && x.Media == null) == 0)
				{
					_logger.LogInformation("TPA Docs completed");
				}
				else
				{
					_logger.LogWarning("TPA Docs ARE NOT completed");
					tpaDocsCompleted = false;
				}
				// (1) Admin Docs
				var adminDocsCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimAdminDocsGroup && x.Media == null) == 0)
				{
					_logger.LogInformation("Admin Docs completed");
				}
				else
				{
					_logger.LogWarning("Admin Docs ARE NOT completed");
					adminDocsCompleted = false;
				}
				// (2) Pictures
				var picturesCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimPicturesGroup && x.Media == null) == 0)
				{
					_logger.LogInformation("Pictures completed");
				}
				else
				{
					_logger.LogWarning("Pictures ARE NOT completed");
					picturesCompleted = false;
				}
				// (3) Pictures x Item
				var picturesXItemsCompleted = true;
				if (claim.ClaimProbatoryDocuments.Count(x => x.GroupId == ClaimPicturesXItemsGroup && x.Media == null) == 0)
				{
					_logger.LogInformation("Pictures x Item(s) completed");
				}
				else
				{
					_logger.LogWarning("Pictures x Item(s) ARE NOT completed");
					picturesXItemsCompleted = false;
				}

				// #####################################
				// 3. Verify the customer has signed off
				// #####################################

				var signatureCompleted = true;
				var signature = await _claimService.GetSignatureByClaimId(arg.ClaimId).ConfigureAwait(false);
				if (signature != null)
				{
					_logger.LogInformation("Signature completed");
				}
				else
				{
					_logger.LogWarning("Signature IS NOT completed");
					signatureCompleted = false;
				}

				// ###########################
				// Build collage if applicable
				// ###########################

				// Prepare the temporary path to save
				var pathToSaveTemp = Path.Combine(_configSettings.TemporaryFilesConfig.PathToSave, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
				if (!_fsa.DirectoryExists(pathToSaveTemp)) _fsa.CreateDirectoryRecursively(pathToSaveTemp);
				var baseUrlTemp = $"{_configSettings.TemporaryFilesConfig.BaseUrl}{DateTime.UtcNow.ToString(@"/yyyy/MM")}";

				var buildCollageResult = await BuildCollage(claim, pathToSaveTemp, baseUrlTemp).ConfigureAwait(false);

				// #################
				// 4. Build PDF File
				// #################

				if (!generalDocsCompleted || !tpaDocsCompleted || !adminDocsCompleted || !picturesCompleted || !picturesXItemsCompleted || !signatureCompleted)
				{
					_logger.LogWarning("Cannot export to PDF due missing documents or signature");
					return;
				}

				// Pull the exporting document settings
				InsuranceExportSettings exportSettings;
                try
                {
					exportSettings = await _insuranceProbatoryDocumentService.GetInsuranceExportSettings(claim.InsuranceCompanyId, claim.ClaimTypeId, EnumExportTypeId.PDF).ConfigureAwait(false);
				}
                catch (Exception ex)
                {
					_logger.LogError($"Cannot pull the exporting settings for insurance company ID {claim.InsuranceCompanyId} and claim type ID {claim.ClaimTypeId} and export type ID {EnumExportTypeId.PDF}", ex);
                    throw;
                }

				// Probatory documents
				var exportProbatoryDocuments = from x in exportSettings.ExportDocuments
											   join c in claim.ClaimProbatoryDocuments on x.ProbatoryDocumentId equals c.ProbatoryDocumentId
											   where x.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument
													&& c.Media != null // Just in case document where removed from the configuration after the order was created and the document was not uploaded
											   select new HostedFile
											   {
												   SortPriority = x.SortPriority,
												   Url = c.Media.Url,
												   Filename = c.Media.Filename,
												   DocumentName = c.ProbatoryDocument.Name,
												   DocumentOrientation = (int)c.ProbatoryDocument.Orientation
											   };

				// Collage(s)
				var exportCollages = from x in exportSettings.ExportDocuments
									 join c in buildCollageResult.Collages on x.CollageId equals c.Id
									 where x.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage
											&& c.Media != null // Here makes sure the collage file was created for real 
									 select new HostedFile
									 {
										 SortPriority = x.SortPriority,
										 Url = c.Media.Url,
										 Filename = c.Media.Filename,
										 DocumentName = c.Name,
										 DocumentOrientation = (int)EnumDocumentOrientationId.NA // NA = Not Applicable (this will prevent fix orientation/rotation)
									 };

				// Build list of documents that will be added to the PDF
				var files = new List<HostedFile>();
				if (exportProbatoryDocuments.Any())
                {
					files.AddRange(exportProbatoryDocuments);
				}
				if (exportCollages.Any())
                {
					files.AddRange(exportCollages);
				}

				// Create PDF file
				var pdfFileCreated = true;
				var newClaimDocument = new Document();
				if (files.Any())
				{
					// Prepare pdf settings
					var pathToSave = Path.Combine(_configSettings.PdfFilesConfig.PathToSave, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
					if (!_fsa.DirectoryExists(pathToSave)) _fsa.CreateDirectoryRecursively(pathToSave);
					var fileName = $"{claim.ExternalOrderNumber.CleanAllSpecialChars().Trim()}_{DateTime.UtcNow.Ticks}_export.pdf";
					var newFileName = _fsa.GenerateUniqueFileName(pathToSave, fileName);
					var baseUrl = $"{_configSettings.PdfFilesConfig.BaseUrl}{DateTime.UtcNow.ToString(@"/yyyy/MM")}";
					var pdfSettings = new CollageAdapter.PdfSettings
					{
						Filename = newFileName,
						OutputFolder = pathToSave,
						BaseUrl = baseUrl,
						ResizeDocumentWidth = _configSettings.MediaEngineConfig.ResizeImageWidth
					};

					// Create the pdf file
					try
					{
						var pdfMedia = _collageAdapter.CreatePdf(files, pdfSettings);

						newClaimDocument.Filename = pdfMedia.Filename;
						newClaimDocument.MimeType = DefaultPdfMimeType;
						newClaimDocument.Url = pdfMedia.Url;
					}
					catch (Exception ex)
					{
						pdfFileCreated = false;
						_logger.LogError(ex, "An error happened in the process of creating the PDF file");
					}
				}

				// ###################################
				// 5. Attach the Pdf File in the Claim (add claim document / document type: pdf (id: 105) / group id: 2).
				// ###################################

				var documentAttached = true;
				ClaimDocument claimDocument = null;
				if (pdfFileCreated)
				{
					try
					{
						claimDocument = await _claimDocumentService.AttachDocument(
							claimId: arg.ClaimId,
							document: newClaimDocument,
							documentTypeId: arg.ClaimDocumentTypeId,
							overwrite: true,
							sortPriority: arg.ClaimDocumentSortPriority,
							groupId: arg.ClaimDocumentGroupId)
							.ConfigureAwait(false);
						_logger.LogInformation("Pdf file successfully attached to the claim order");
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "An error happened when attached the PDF file in the claim order");
						documentAttached = false;
					}
				}

				// ##################################
				// 6. SignalR and Email notifications
				// ##################################

				if (pdfFileCreated && documentAttached)
				{
					// SignalR
					var message = new MessagePdfClaimFilesReady
					{
						Title = "Archivo PDF Generado",
						Content = @$"Taller: {claim.Store.Name}{htmlNewLine}AGRI No.: {claim.ExternalOrderNumber}",
						Url = newClaimDocument.Url,
						HasUrl = true,
						ClaimId = claim.Id,
						DocumentId = claimDocument.DocumentId,
						HasClaim = true
					};
					var notification = new Notification
					{
						Source = nameof(ClaimProbatoryDocumentsJob),
						Target = EnumUserRoleId.Admin.ToString(),
						Type = EnumNotificationTypeId.GroupMessage,
						MessageType = DefaulPdfMessageType,
						Message = JsonConvert.SerializeObject(message),
                    };
					try
					{
						var notificationResult = await _notificationService.SendNotification(notification).ConfigureAwait(false);
						_logger.LogInformation("The notification to SignalR was successful", notificationResult);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "The notification to SignalR failed");
						throw;
					}
					// Email
					// ...
				}

				// ###
				// END
				// ###
			}
		}

		private async Task<BuildCollageResult> BuildCollage(Claim claim, string pathToSaveTemp, string baseUrlTemp)
        {
			var collages = new List<InsuranceCollage>();
			try
			{
				var result = await _insuranceCollageService.GetInsuranceCollageListByInsuranceCompanyIdAndClaimTypeId(claim.InsuranceCompanyId, claim.ClaimTypeId).ConfigureAwait(false);
				if (result.Any())
				{
					collages = result.ToList();
				}
			}
			catch (NonExistingRecordCustomizedException ex)
			{
				_logger.LogInformation(ex, $"Couldn't find collages for the Company ID {claim.InsuranceCompanyId} and Claim Type ID {claim.ClaimTypeId}");
			}
			catch (EndpointRequestCustomizedException ex)
			{
				_logger.LogError(ex, "Couldn't complete request to external service of InsuranceCollageService");
				throw;
			}

			// Prepare the collage probatory documents and media files
			// IMPORTANT: The documents collected here will be excluded from the ZIP file
			var claimProbatoryDocumentsAddedToCollages = new List<ClaimProbatoryDocument>();

			// Extract the probatory document IDs from the collages
			var collageProbatoryDocumentIds = collages.SelectMany(collage => collage.ProbatoryDocuments)?.Select(x => x.ProbatoryDocumentId).Distinct();
			if (collageProbatoryDocumentIds.Any())
			{
				// Build the list of probatory documents that exist in the collage
				var query = from cpd in claim.ClaimProbatoryDocuments
							where collageProbatoryDocumentIds.Contains(cpd.ProbatoryDocumentId)
							orderby cpd.GroupId, cpd.ClaimItemId, cpd.SortPriority
							select cpd;

				claimProbatoryDocumentsAddedToCollages = query.ToList();

				// NOTES: Uncomment below lines only if you need change the name of the probatory documents
				// i.e. Cristal 1 - Foto DOT
				//claimProbatoryDocumentsAddedToCollages.ForEach(x =>
				//{
				//	if (x.GroupId == 3 && x.ClaimItemId.HasValue)
				//	{
				//		x.ProbatoryDocument.Name = $"Cristal {x.ClaimItemId.Value} - {x.ProbatoryDocument.Name}";
				//	}
				//});
			}

            //// Prepare the temporary path to save
            //var pathToSaveTemp = Path.Combine(_configSettings.TemporaryFilesConfig.PathToSave, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
            //if (!_fsa.DirectoryExists(pathToSaveTemp)) _fsa.CreateDirectoryRecursively(pathToSaveTemp);
            //var baseUrlTemp = $"{_configSettings.TemporaryFilesConfig.BaseUrl}{DateTime.UtcNow.ToString(@"/yyyy/MM")}";

            // Generate collage file and assign to collage.Media (consider it could be multiple collages)
            foreach (var collage in collages)
			{
				// Get the list of filenames of the probatory documents that exist in the collage
				//var filenames = claimProbatoryDocumentsAddedToCollages.Where(claimProbatoryDoc => collage.ProbatoryDocuments.Any(collageProbatoryDoc => collageProbatoryDoc.ProbatoryDocumentId == claimProbatoryDoc.ProbatoryDocumentId)
				//		&& !claimProbatoryDoc.Media.Filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
				//	.Select(doc => doc.Media.Filename)
				//	.ToArray();

				var collageImages = claimProbatoryDocumentsAddedToCollages.Where(claimProbatoryDoc => collage.ProbatoryDocuments.Any(collageProbatoryDoc => collageProbatoryDoc.ProbatoryDocumentId == claimProbatoryDoc.ProbatoryDocumentId)
						&& !claimProbatoryDoc.Media.Filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
					.Select(doc => new CollageAdapter.CollageImage
					{
						Filename = doc.Media.Filename,
						Orientation = doc.ProbatoryDocument.Orientation == EnumDocumentOrientationId.NA ? Aide.Core.Media.Enumerations.CollageImageOrientation.NA : doc.ProbatoryDocument.Orientation == EnumDocumentOrientationId.Portrait ? Aide.Core.Media.Enumerations.CollageImageOrientation.Portrait : Aide.Core.Media.Enumerations.CollageImageOrientation.Landscape
					})
					.ToArray();

				if (collageImages.Any())
				{
					// Prepare collage settings
					var fileName = $"{claim.ExternalOrderNumber.CleanAllSpecialChars().Trim()}_{DateTime.UtcNow.Ticks}_collage.jpg";
					var newFileName = _fsa.GenerateUniqueFileName(pathToSaveTemp, fileName);
					var baseUrl = baseUrlTemp;
					var collageSettings = new CollageAdapter.CollageSettings
					{
						Columns = collage.Columns,
						MimeType = CollageMimeType,
						Filename = newFileName,
						OutputFolder = pathToSaveTemp,
						BaseUrl = baseUrl
					};

					// Create the media and assign the file to the collage
					var collageMedia = _collageAdapter.CreateCollage(collageImages, collageSettings);
					collage.Media = new Media
					{
						Filename = collageMedia.Filename,
						Url = collageMedia.Url,
						MimeType = collageMedia.MimeType,
						DateCreated = collage.DateCreated,
						DateModified = collageMedia.DateModified
					};
				}
			}

			return new BuildCollageResult
			{
				Collages = collages,
				ClaimProbatoryDocumentsAddedToCollages = claimProbatoryDocumentsAddedToCollages
			};
		}

		#region Local Classes

		public class ConfigSettings
		{
			public PdfFilesConfig PdfFilesConfig { get; set; }
			public ZipFilesConfig ZipFilesConfig { get; set; }
			public ReceiptDocumentConfig ReceiptDocumentConfig { get; set; }
			public TemporaryFilesConfig TemporaryFilesConfig { get; set; }
			public MediaEngineConfig MediaEngineConfig { get; set; }
		}

		public class PdfFilesConfig
		{
			public string PathToSave { get; set; }
			public string BaseUrl { get; set; }
		}

		public class ZipFilesConfig
		{
			public string PathToSave { get; set; }
			public string BaseUrl { get; set; }
		}

		public class ReceiptDocumentConfig
		{
			public int DocumentTypeId { get; set; }
			public int GroupId { get; set; }
			public int SortPriority { get; set; }
		}

		public class TemporaryFilesConfig
		{
			public string PathToSave { get; set; }
			public string BaseUrl { get; set; }
		}

		public class MediaEngineConfig
		{
			public int ResizeImageWidth { get; set; }
		}

		private class BuildCollageResult
        {
			public IEnumerable<InsuranceCollage> Collages { get; set; }
			public IEnumerable<ClaimProbatoryDocument> ClaimProbatoryDocumentsAddedToCollages { get; set; }
		}

		#endregion
	}
}
