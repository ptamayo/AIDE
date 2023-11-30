using Aide.Core.CustomExceptions;
using Aide.Core.Domain;
using Aide.Core.Interfaces;
using Aide.Hangfire.Common.Messages;
using Aide.Hangfire.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs
{
    public class ZipFilesManagementJob
    {
        private readonly ILogger<ZipFilesManagementJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserService _userService;
        private readonly IZipArchiveAdapter _zipArchiveAdapter;
        private readonly ZipFilesManagementJobConfig _jobConfig;
        private readonly IFileSystemAdapter _fsa;

        public ZipFilesManagementJob(
            ILogger<ZipFilesManagementJob> logger,
            IServiceProvider serviceProvider,
            IUserService userService,
            IZipArchiveAdapter zipArchiveAdapter,
            ZipFilesManagementJobConfig jobConfig,
            IFileSystemAdapter fsa
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _zipArchiveAdapter = zipArchiveAdapter ?? throw new ArgumentNullException(nameof(zipArchiveAdapter));
            _jobConfig = jobConfig ?? throw new ArgumentNullException(nameof(jobConfig));
            _fsa = fsa ?? throw new ArgumentNullException(nameof(fsa));
        }

        public async Task ZipFiles(ZipFilesMessage arg)
        {
            if (arg == null) throw new ArgumentNullException(nameof(arg));
            if (arg.Files == null) throw new ArgumentNullException(nameof(arg.Files));
            if (!arg.Files.Any()) throw new ArgumentException(nameof(arg.Files));
            if (string.IsNullOrWhiteSpace(arg.OutputFolder)) throw new ArgumentNullException(arg.OutputFolder);
            if (string.IsNullOrWhiteSpace(arg.CallbackTypeId)) throw new ArgumentNullException(nameof(arg.CallbackTypeId));
            if (string.IsNullOrWhiteSpace(arg.Callback)) throw new ArgumentNullException(nameof(arg.Callback));

            // If the output filename is not provided then build one.
            
            if (string.IsNullOrWhiteSpace(arg.OutputFilename))
            {
                arg.OutputFilename = $"{DateTime.UtcNow.Ticks}.zip";
            }

            // Add sub-folders at the end of the output folder for better organization.

            var outputFolder = Path.Combine(arg.OutputFolder, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));

            // Verify if the output folder exists, if not then create it.

            if (!_fsa.DirectoryExists(outputFolder))
            {
                _fsa.CreateDirectoryRecursively(outputFolder);
            }

            // Build the full path for the output file.

            var fullPath = Path.Combine(outputFolder, arg.OutputFilename);

            // Verify each file exists and build a collection of files to zip.

            var files = new List<HostedFile>();
            foreach(var filename in arg.Files)
            {
                var fileExists = _fsa.FileExists(filename);
                if (fileExists)
                {
                    var fileInfo = _fsa.GetFileInfo(filename);
                    var file = new HostedFile
                    {
                        Filename = filename,
                        DocumentName = fileInfo.Name,
                        DocumentOrientation = 0,
                        SortPriority = 0,
                        Url = fileInfo.Name
                    };
                    files.Add(file);
                }
                else
                {
                    _logger.LogWarning($"The following file WON'T be zipped because it does NOT exist: {filename}");
                }
            }

            if (!files.Any())
            {
                var message = "There are no files ready to ZIP.";
                _logger.LogWarning(message);
                throw new NonExistingRecordCustomizedException(message);
            }

            _zipArchiveAdapter.ZipHostedFileList(files, fullPath);

            // Push the response back to the caller.

            if (arg.CallbackTypeId == "Endpoint")
            {
                // Prepare the response for the caller.
                var payload = new
                {
                    OutputFilename = fullPath
                };

                // Serialize the payload.
                var payloadJsonString = JsonConvert.SerializeObject(payload);

                // Prepare the request.
                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(arg.Callback));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(payloadJsonString, Encoding.UTF8);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // Add the token.
                var userAuth = await _userService.Authenticate(_jobConfig.Credentials.Username, _jobConfig.Credentials.HashedPsw).ConfigureAwait(false);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.Token);

                // Initialize the http client using the http client factory.
                var clientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
                var client = clientFactory.CreateClient();

                // Submit the request.
                var response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var message = $"The ZipFilesManagementJob failed to push the notification back to the caller.\r\n{payloadJsonString}";
                    _logger.LogError(message);
                    throw new Exception(message);
                }
            }
            else
            {
                _logger.LogWarning($"The files were successfully zipped BUT no caller it's been notified because the caller info is missing.");
            }
        }

        #region Local classes

        public class ZipFilesManagementJobConfig
        {
            public ServiceCredentialsConfig Credentials { get; set; }
        }

        #endregion
    }
}
