using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Aide.Core.CustomExceptions;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Services;
using Aide.Claims.WebApi.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Aide.Claims.WebApi.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class ClaimProbatoryDocumentController : ControllerBase
    {
        private readonly ILogger<ClaimProbatoryDocumentController> _logger;
        private readonly IClaimProbatoryDocumentService _claimProbatoryDocumentService;
        private readonly IMediaService _mediaService;
        private readonly IFileSystemAdapter _fsa;
        private readonly IContentTypeProvider _contentTypeProvider;
        private readonly AppSettings _appSettings;

        public ClaimProbatoryDocumentController(
            ILogger<ClaimProbatoryDocumentController> logger,
            IClaimProbatoryDocumentService claimProbatoryDocumentService,
            IMediaService mediaService,
            IFileSystemAdapter fsa,
            IContentTypeProvider contentTypeProvider,
            AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _claimProbatoryDocumentService = claimProbatoryDocumentService ?? throw new ArgumentNullException(nameof(claimProbatoryDocumentService));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _fsa = fsa ?? throw new ArgumentNullException(nameof(fsa));
            _contentTypeProvider = contentTypeProvider ?? throw new ArgumentNullException(nameof(contentTypeProvider));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        [HttpGet("media")]
        public async Task<IActionResult> DownloadMedia([FromQuery] int h)
        {
            if (h <= 0) return BadRequest();

            string filename;
            try
            {
                var media = await _mediaService.GetMediaById(h);
                filename = media.Filename;
            }
            catch (NonExistingRecordCustomizedException)
            {
                _logger.LogWarning($"Couldn't get the media file {h}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the media file {h}");
                throw;
            }

            string contentType;
            if (!_contentTypeProvider.TryGetContentType(filename, out contentType))
            {
                _logger.LogError($"Couldn't get the content type of the media file {filename}");
                throw new Exception($"Couldn't get the content type of the media file {filename}");
            }

            var mediaResult = _fsa.FileReadAllBytes(filename);
            if (mediaResult.OperationCompletedSuccessfully)
            {
                return File((byte[])mediaResult.Value, contentType);
            }
            else
            {
                _logger.LogError($"Couldn't load the media file {filename}: {mediaResult.Message}");
                throw new Exception($"Couldn't load the media file {filename}", new Exception(mediaResult.Message));
            }
        }

        [HttpPost("media"), DisableRequestSizeLimit]
        public async Task<IActionResult> Upload()
        {
            try
            {
                var claimProbatoryDocumentId = Convert.ToInt32(Request.Form["claimProbatoryDocumentId"]);
                var file = Request.Form.Files[0];
                var pathToSave = Path.Combine(_appSettings.MediaFilesConfig.PathToSave, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));

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

                    var media = new Media
                    {
                        Filename = fullPath,
                        MimeType = file.ContentType,
                    };
                    await _claimProbatoryDocumentService.AttachMedia(media, claimProbatoryDocumentId);

                    return Ok(media);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't upload a file");
                throw;
            }
        }

        /// <summary>
		/// This is for INTERNAL/PRIVATE use only and SHOULD NOT be exposed to the PUBLIC in the API GATEWAY
		/// </summary>
		/// <param name="request">AttachProbatoryDocumentRequest</param>
		/// <returns></returns>
        [HttpPost("attach")]
        public async Task<IActionResult> PrivatelyAttachProbatoryDocument(AttachProbatoryDocumentRequest request)
        {
            if (request == null) return BadRequest();
            if (request.ClaimProbatoryDocumentId <= 0) return BadRequest();
            if (request.Media == null) return BadRequest();

            try
            {
                var claimProbatoryDocumentMedia = await _claimProbatoryDocumentService.AttachMedia(request.Media, request.ClaimProbatoryDocumentId);
                return Ok(claimProbatoryDocumentMedia);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't attach the probatory document {request.Media.Filename}");
                throw;
            }
        }

        [HttpPost("{probatoryDocumentId}/exists")]
        public async Task<IActionResult> ExistProbatoryDocumentInClaimId([FromRoute] int probatoryDocumentId, [FromBody] ExistProbatoryDocumentRequest request)
        {
            if (probatoryDocumentId <= 0) return BadRequest();
            if (request == null) return BadRequest();
            if (request.ClaimId <= 0) return BadRequest();

            var claimProbatoryDocuments = await _claimProbatoryDocumentService.GetAllClaimProbatoryDocumentsByClaimId(request.ClaimId);
            if (claimProbatoryDocuments == null)
            {
                var message = $"Couldn't find any probatory documents related to the claim ID {request.ClaimId}.";
                _logger.LogWarning(message);
                return Ok(new ExistProbatoryDocumentResponse
                {
                    IsOperationSuccessful = false,
                    Message = message
                });
            }

            var probatoryDocument = claimProbatoryDocuments.FirstOrDefault(x => x.ProbatoryDocumentId == probatoryDocumentId);
            if (probatoryDocument == null)
            {
                var message = $"Couldn't find the probatory document ID {probatoryDocumentId} associated to the claim ID {request.ClaimId}.";
                _logger.LogWarning(message);
                return Ok(new ExistProbatoryDocumentResponse
                {
                    IsOperationSuccessful = false,
                    Message = message
                });
            }

            return Ok(new ExistProbatoryDocumentResponse
            {
                IsOperationSuccessful = true,
                ClaimProbatoryDocumentId = probatoryDocument.Id
            });
        }

        [HttpGet("{claimId}")]
        public async Task<IActionResult> GetListOfProbatoryDocumentsByClaimId([FromRoute] int claimId)
        {
            if (claimId <= 0) return BadRequest();

            IEnumerable<ClaimProbatoryDocument> claimProbatoryDocuments;

            try
            {
                claimProbatoryDocuments = await _claimProbatoryDocumentService.GetClaimProbatoryDocumentListByClaimIds(new int[] { claimId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get the list of the claim probatory documents for the claim ID {claimId}");
                throw;
            }

            if (!claimProbatoryDocuments.Any())
            {
                return NoContent();
            }

            var result = from x in claimProbatoryDocuments
                         select new AideClaimDocument
                         {
                             ProbatoryDocumentId = x.ProbatoryDocument.Id,
                             ProbatoryDocumentName = x.ProbatoryDocument.Name,
                             MediaId = x.Media?.Id,
                             GroupId = x.GroupId,
                             ClaimItemId = x.ClaimItemId
                         };

            return Ok(result);
        }

        #region Local classes

        public class AttachProbatoryDocumentRequest
        {
            public int ClaimProbatoryDocumentId { get; set; }
            public Media Media { get; set; }
        }

        public class ExistProbatoryDocumentRequest
        {
            public int ClaimId { get; set; }
        }

        public class ExistProbatoryDocumentResponse
        {
            public bool IsOperationSuccessful { get; set; }
            public int ClaimProbatoryDocumentId { get; set; }
            public string Message { get; set; }
        }

        #endregion
    }
}