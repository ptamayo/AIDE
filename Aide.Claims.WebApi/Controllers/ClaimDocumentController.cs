using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Aide.Core.Interfaces;
using Aide.Claims.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Document = Aide.Claims.Domain.Objects.Document;
using Microsoft.Extensions.Logging;
using Aide.Core.CustomExceptions;
using Microsoft.AspNetCore.StaticFiles;
using System.Linq;
using Aide.Hangfire.Common.Messages;
using Aide.Claims.WebApi.Adapters;

namespace Aide.Claims.WebApi.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class ClaimDocumentController : ControllerBase
    {
        private readonly ILogger<ClaimDocumentController> _logger;
        private readonly IClaimDocumentService _claimDocumentService;
        private readonly IDocumentService _documentService;
        private readonly IFileSystemAdapter _fsa;
        private readonly IContentTypeProvider _contentTypeProvider;
        private readonly AppSettings _appSettings;
        private readonly IMessageBrokerAdapter _messageBrokerAdapter;

        public ClaimDocumentController(
            ILogger<ClaimDocumentController> logger, 
            IClaimDocumentService claimDocumentService,
            IDocumentService documentService,
            IFileSystemAdapter fsa,
            IContentTypeProvider contentTypeProvider,
            AppSettings appSettings,
            IMessageBrokerAdapter messageBrokerAdapter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _claimDocumentService = claimDocumentService ?? throw new ArgumentNullException(nameof(claimDocumentService));
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _fsa = fsa ?? throw new ArgumentNullException(nameof(fsa));
            _contentTypeProvider = contentTypeProvider ?? throw new ArgumentNullException(nameof(contentTypeProvider));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _messageBrokerAdapter = messageBrokerAdapter ?? throw new ArgumentNullException(nameof(messageBrokerAdapter));
        }

        [HttpGet("document")]
        public async Task<IActionResult> DownloadDocument([FromQuery] int h)
        {
            if (h <= 0) return BadRequest();

            string filename;
            try
            {
                var document = await _documentService.GetDocumentById(h);
                filename = document.Filename;
            }
            catch (NonExistingRecordCustomizedException)
            {
                _logger.LogWarning($"Couldn't get the document file {h}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the document file {h}");
                throw;
            }

            string contentType;
            if (!_contentTypeProvider.TryGetContentType(filename, out contentType))
            {
                _logger.LogError($"Couldn't get the content type of the document file {filename}");
                throw new Exception($"Couldn't get the content type of the document file {filename}");
            }

            var documentResult = _fsa.FileReadAllBytes(filename);
            if (documentResult.OperationCompletedSuccessfully)
            {
                return File((byte[])documentResult.Value, contentType);
            }
            else
            {
                _logger.LogError($"Couldn't load the document file {filename}: {documentResult.Message}");
                throw new Exception($"Couldn't load the document file {filename}", new Exception(documentResult.Message));
            }
        }

        [HttpPost("document"), DisableRequestSizeLimit]
        public async Task<IActionResult> Upload()
        {
            try
            {
                var claimId = Convert.ToInt32(Request.Form["claimId"]);
                var documentTypeId = Convert.ToInt32(Request.Form["documentTypeId"]);
                var overwrite = Convert.ToBoolean(Request.Form["overwrite"]);
                var sortPriority = Convert.ToInt32(Request.Form["sortPriority"]);
                var groupId = Convert.ToInt32(Request.Form["groupId"]);
                var file = Request.Form.Files[0];
                var pathToSave = Path.Combine(_appSettings.DocumentFilesConfig.PathToSave, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));

                if (!_fsa.DirectoryExists(pathToSave)) _fsa.CreateDirectoryRecursively(pathToSave);

                if (file.Length > 0)
                {
                    var fileContentDisposition = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
                    var fileExtension = Path.GetExtension(fileContentDisposition.FileName.Trim('\"'));
                    var fileName = DateTime.UtcNow.Ticks.ToString() + fileExtension;
                    var newFileName = _fsa.GenerateUniqueFileName(pathToSave, fileName);
                    var fullPath = Path.Combine(pathToSave, newFileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    var document = new Document
                    {
                        Filename = fullPath,
                        MimeType = file.ContentType,
                    };
                    var claimDocument = await _claimDocumentService.AttachDocument(claimId, document, documentTypeId, overwrite, sortPriority, groupId);

                    return Ok(claimDocument);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't upload document");
                throw;
            }
        }

        /// <summary>
		/// This is for INTERNAL/PRIVATE use only and SHOULD NOT be exposed to the PUBLIC in the API GATEWAY
		/// </summary>
		/// <param name="request">AttachDocumentRequest</param>
		/// <returns></returns>
        [HttpPost("attach")]
        public async Task<IActionResult> PrivatelyAttachDocument(AttachDocumentRequest request)
		{
            if (request == null) return BadRequest();
            if (request.ClaimId <= 0) return BadRequest();
            if (request.Document == null) return BadRequest();
            if (request.DocumentTypeId <= 0) return BadRequest();
            if (request.SortPriority <= -1) return BadRequest();
            if (request.GroupId <= -1) return BadRequest();

			try
			{
                var claimDocument = await _claimDocumentService.AttachDocument(request.ClaimId, request.Document, request.DocumentTypeId, request.Overwrite, request.SortPriority, request.GroupId);
                return Ok(claimDocument);
            }
			catch (Exception ex)
			{
                _logger.LogError(ex, $"Couldn't attach document {request.Document.Filename}");
                throw;
			}
		}

        /// <summary>
        /// This is for INTERNAL/PRIVATE use only and SHOULD NOT be exposed to the PUBLIC in the API GATEWAY
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("enqueueZipRequest")]
        public async Task<IActionResult> EnqueueZipRequest([FromBody] ZipFilesRequest request)
        {
            if (request == null) return BadRequest();
            if (request.Files == null) return BadRequest();
            if (!request.Files.Any()) return BadRequest();
            if (string.IsNullOrWhiteSpace(request.OutputFolder)) return BadRequest();
            if (string.IsNullOrWhiteSpace(request.CallbackTypeId)) return BadRequest();
            if (string.IsNullOrWhiteSpace(request.Callback)) return BadRequest();

            // Re-send the request to hangfire.

            var zipFilesMessageRequest = request.ToZipFilesMessage();
            await _messageBrokerAdapter.SendZipFilesMessage(zipFilesMessageRequest);

            // Return a successful response to the caller.

            return Ok(new AideZipFilesResponse
            {
                IsOperationSuccessful = true
            });
        }

        #region Local classes

        public class AttachDocumentRequest
        {
            public int ClaimId { get; set; }
            public Document Document { get; set; }
            public int DocumentTypeId { get; set; }
            public bool Overwrite { get; set; }
            public int SortPriority { get; set; }
            public int GroupId { get; set; }
        }

        public class ZipFilesRequest
        {
            public string[] Files { get; set; }
            public string OutputFolder { get; set; }
            public string OutputFilename { get; set; }
            public string CallbackTypeId { get; set; }
            public string Callback { get; set; }
            public object Metadata { get; set; }
        }

        public class AideZipFilesResponse
        {
            public bool IsOperationSuccessful { get; set; }
            public string Message { get; set; }
        }

        #endregion
    }

    #region Extension methods

    public static class ZipFilesRequestExtensions
    {
        public static ZipFilesMessage ToZipFilesMessage(this ClaimDocumentController.ZipFilesRequest request)
        {
            return new ZipFilesMessage
            {
                Files = request.Files,
                OutputFolder = request.OutputFolder,
                OutputFilename = request.OutputFilename,
                CallbackTypeId = request.CallbackTypeId,
                Callback = request.Callback,
                Metadata = request.Metadata
            };
        }
    }

    #endregion
}