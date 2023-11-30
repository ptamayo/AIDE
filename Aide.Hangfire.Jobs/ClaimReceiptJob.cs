using Aide.Core.Cloud.Azure.SendGrid;
using Aide.Core.Extensions;
using Aide.Core.Interfaces;
using Aide.Hangfire.Common.Messages;
using Aide.Hangfire.Domain.Enumerations;
using Aide.Hangfire.Domain.Objects;
using Aide.Hangfire.Domain.SignalRMessages;
using Aide.Hangfire.Jobs.Settings;
using Aide.Hangfire.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace Aide.Hangfire.Jobs
{
    public class ClaimReceiptJob
	{
		private readonly ILogger<ClaimReceiptJob> _logger;
		private readonly IClaimService _claimService;
		private readonly ISendGridClientAdapter _sendGridClient;
		private readonly IClaimProbatoryDocumentService _claimProbatoryDocumentService;
		private readonly IFileSystemAdapter _fsa;
		private readonly ClaimReceiptJobConfig _configSettings;
		private readonly INotificationService _notificationService;

		private const string DefaultPdfMimeType = "application/pdf";
		private const string DefaultJsonMimeType = "application/json";
		private const string DefaulMessageType = "ClaimReceiptReady";
		private const string htmlNewLine = "<br/>";
		private const string notifyWhenCustomerSignatureIsCompletedQueue = "NotifyWhenCustomerSignatureIsCompleted";

		public ClaimReceiptJob(
			ILogger<ClaimReceiptJob> logger,
			IClaimService claimService,
			ISendGridClientAdapter sendGridClientAdapter,
			IClaimProbatoryDocumentService claimProbatoryDocumentService,
			IFileSystemAdapter fsa,
			ClaimReceiptJobConfig configSettings,
			INotificationService notificationService)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_claimService = claimService ?? throw new ArgumentNullException(nameof(claimService));
			_sendGridClient = sendGridClientAdapter ?? throw new ArgumentNullException(nameof(sendGridClientAdapter));
			_claimProbatoryDocumentService = claimProbatoryDocumentService ?? throw new ArgumentNullException(nameof(claimProbatoryDocumentService));
			_fsa = fsa ?? throw new ArgumentNullException(nameof(fsa));
			_configSettings = configSettings ?? throw new ArgumentNullException(nameof(configSettings));
			_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
		}

		public async Task SendEmailAsync(EmailClaimReceiptMessage arg)
		{
			try
			{
				var signature = await _claimService.GetSignatureByClaimId(arg.ClaimId).ConfigureAwait(false);
                var fixedBase64Image = signature.Base64image.Split(',')[1];
                var signatureByteArray = Convert.FromBase64String(fixedBase64Image);

                QuestPDF.Settings.License = LicenseType.Community;
                var pdfDocument = QuestPDF.Fluent.Document.Create(container =>
				{
					container.Page(page =>
					{
                        page.Size(PageSizes.Letter);
						page.MarginLeft(2, Unit.Centimetre);
						page.MarginRight(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(8));

						//page.Header(); // Not applicable at this time.

						page.Content()
							.PaddingVertical(1, Unit.Centimetre)
							.Column(x =>
							{
								x.Item().Text(text =>
								{
									text.Line($"Recibo de orden {arg.ExternalOrderNumber}").FontSize(12).Bold();
									text.EmptyLine();
									text.Line("Muy Señores Míos (Nuestros):");
                                    text.EmptyLine();
                                    text.Line("Me (nos) es grato manifestarles que la reclamación que tengo (tenemos) presentada a esa compañía con motivo del accidente en el cual fue implicado el automóvil de mi (nuestra) propiedad, es grato manisfestar que dicha reclamación ha sido debidamente atendida por ustedes y terminada a mi (nuestra) entera satisfacción, en vista de lo cual me (nos) es grato hacer constar por medio de la presente que relevo (relevamos) a ustedes y a sus representantes de cuaquier responsabilidad posterior con motivo de la citada reclamación.");
                                    text.EmptyLine();
                                    text.Line("Además constato (constatamos) que conozco (conocemos) y entiendo (entendemos) cuáles son los documentos fotocopiados y/o digitalizados por ustedes para fines administrativos del servicio proporcionado. La lista de documentos y fotografías proporcionados se listan a continuación:");
								});

                                foreach (var docName in arg.ClaimProbatoryDocuments)
                                {
                                    x.Item().Row(row =>
                                    {
                                        row.Spacing(5);
                                        row.AutoItem().Text($"•");
                                        row.RelativeItem().Text(docName);
                                    });
                                }
                                
								x.Item().Text(text =>
								{
                                    text.EmptyLine();
                                    text.Line("Sin más de momento, me (nos) es grato repetirme (nos) de ustedes Afmo. (s), Atto. (s) y S.S. (SS).");
                                    text.EmptyLine();
                                });
                                x.Item().AlignCenter().Image(signatureByteArray);
								x.Item().Text(text =>
								{
									text.AlignCenter();
									text.Line(string.Concat(System.Linq.Enumerable.Repeat(" ", 200))).Underline();
                                    text.EmptyLine();
                                    text.Line($"{arg.CustomerFullName}").Bold();
                                    text.EmptyLine();
                                    text.Line($"{signature.LocalDate}");
                                    text.Line($"Zona horaria: {signature.LocalTimeZone}");
                                });
							});

                        //page.Footer(); // Not applicable at this time.
                    });
				});

				// Save PDF Receipt Locally
				var receiptFileCreated = true;
				var newClaimProbatoryDocument = new Media();
				try
				{
					var pathToSave = Path.Combine(_configSettings.PdfFilesConfig.PathToSave, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
					if (!_fsa.DirectoryExists(pathToSave)) _fsa.CreateDirectoryRecursively(pathToSave);
					var fileExtension = ".pdf";
					var fileName = $"{arg.ExternalOrderNumber.CleanAllSpecialChars().Trim()}_{DateTime.UtcNow.Ticks}{fileExtension}";
					var newFileName = _fsa.GenerateUniqueFileName(pathToSave, fileName);
					var fullPath = Path.Combine(pathToSave, newFileName);
					// .....
					pdfDocument.GeneratePdf(fullPath);
                    // .....
                    _logger.LogInformation("PDF receipt file successfully created");

					newClaimProbatoryDocument.Filename = fullPath;
					newClaimProbatoryDocument.MimeType = DefaultPdfMimeType;
					newClaimProbatoryDocument.Url = $"{_configSettings.PdfFilesConfig.BaseUrl}{DateTime.UtcNow.ToString(@"/yyyy/MM/")}{newFileName}";
				}
				catch (Exception ex)
				{
					receiptFileCreated = false;
					_logger.LogError(ex, "An error happened in the process of creating the PDF receipt file");
				}

				// Attach the PDF Receipt in the Claim
				ClaimProbatoryDocumentMedia claimDocument = null;
				if (receiptFileCreated)
				{
					try
					{
						var request = new ClaimProbatoryDocumentService.AttachProbatoryDocumentRequest
						{
							ClaimProbatoryDocumentId = arg.ClaimProbatoryDocumentId,
							Media = newClaimProbatoryDocument
						};
						claimDocument = await _claimProbatoryDocumentService.AttachProbatoryDocument(request).ConfigureAwait(false);
						_logger.LogInformation("PDF receipt file successfully attached to the claim order");
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "An error happened when attached the PDF receipt file in the claim order");
					}
				}

                var claim = await _claimService.GetClaimById(arg.ClaimId).ConfigureAwait(false);

                // SignalR notification:
                // IMPORTANT: Notice this is also conditioned to the setting of sending the receipt via email.
                // It would be good to introduce a new setting for this separated of the emails.
                if (_configSettings.SendGridConfig.ClaimReceiptEmailEnabled)
				{
					var message = new MessageClaimReceiptReady
					{
						Title = "Recibo Generado",
						Content = @$"Taller: {claim.Store.Name}{htmlNewLine}AGRI No.: {claim.ExternalOrderNumber}",
						Url = newClaimProbatoryDocument.Url,
						HasUrl = true,
						ClaimId = claim.Id,
						MediaId = claimDocument.MediaId,
						HasClaim = true,
						HasDepositSlip = claim.HasDepositSlip
					};
					var notification = new Notification
					{
						Source = nameof(ClaimReceiptJob),
						Target = EnumUserRoleId.Admin.ToString(),
						Type = EnumNotificationTypeId.GroupMessage,
						MessageType = DefaulMessageType,
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
				}
                else
                {
                    _logger.LogWarning($"The setting for pushing claim receipts notifications is OFF. The receipt for the claim ID {arg.ClaimId} won't be notified via signalR.");
                }

                //// SAS+ Notification:
                //// Send aide receipt completed notification to SAS+
                //// IMPORTANT: This should NOT apply to orders created for AGRI. i.e. MJ0961390
                //if (!(claim.ExternalOrderNumber.StartsWith("MJ") && claim.ExternalOrderNumber.Length == 9))
                //{
                //	if (_configSettings.ThirdPartySystemNotifications.ContainsKey(notifyWhenCustomerSignatureIsCompletedQueue))
                //	{
                //		var queueUrl = _configSettings.ThirdPartySystemNotifications[notifyWhenCustomerSignatureIsCompletedQueue].Queue;
                //		var aideNotificationMessage = new AideNotificationMessage
                //		{
                //			Action = "AideReceiptComplete",
                //			ClaimId = claim.Id,
                //			Message = "Aide receipt completed",
                //			TransactionId = Guid.NewGuid().ToString()
                //		};
                //		await _messageBrokerAdapter.SendNotificationMessageToThirdPartySystem(aideNotificationMessage, queueUrl).ConfigureAwait(false);
                //	}
                //}
            }
            catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't send a claim receipt email due to an unhandled error.");
				throw;
			}
		}

		#region Local Classes

		public class ClaimReceiptJobConfig
		{
            public bool IsEmailServiceEnabled { get; set; }
            public string EmailFrom { get; set; }
			public string PilkingtonTpaEmail { get; set; }
			public SendGridConfig SendGridConfig { get; set; }
			public PdfFilesConfig PdfFilesConfig { get; set; }
			public Dictionary<string, ThirdPartySystemNotificationQueue> ThirdPartySystemNotifications { get; set; }
		}

		public class PdfFilesConfig
		{
			public string PathToSave { get; set; }
			public string BaseUrl { get; set; }
		}

		public class ThirdPartySystemNotificationQueue
		{
			public bool Enabled { get; set; }
			public string Queue { get; set; }
		}

		#endregion
	}
}
