using Aide.Core.Data;
using Aide.Hangfire.Common.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Aide.Reports.Models;
using System.Linq;
using Aide.Core.Interfaces;
using CsvHelper.Configuration.Attributes;
using Aide.Hangfire.Domain.SignalRMessages;
using Aide.Hangfire.Domain.Objects;
using Aide.Hangfire.Domain.Enumerations;
using Aide.Hangfire.Services;
using Aide.Core.Extensions;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.IO;

namespace Aide.Hangfire.Jobs
{
    public class ReportJob
    {
        private readonly ILogger<ReportJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICsvAdapter _csvAdapter;
        private readonly ITimeZoneAdapter _timeZoneAdapter;
        private readonly IFileSystemAdapter _fsa;
        private readonly INotificationService _notificationService;
        private readonly ConfigSettings _configSettings;
        private const string DefaulMessageType = "Dashboard1ClaimsReportReady";
        private const string DefaultDateTimeFormat = "dd/MM/yyyy HH:mm:ss";

        public ReportJob(ILogger<ReportJob> logger, 
            IServiceProvider serviceProvider, 
            ICsvAdapter csvAdapter, 
            ITimeZoneAdapter timeZoneAdapter,
            IFileSystemAdapter fsa,
            INotificationService notificationService, 
            ConfigSettings configSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _csvAdapter = csvAdapter ?? throw new ArgumentNullException(nameof(csvAdapter));
            _timeZoneAdapter = timeZoneAdapter ?? throw new ArgumentNullException(nameof(timeZoneAdapter));
            _fsa = fsa ?? throw new ArgumentNullException(nameof(fsa));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _configSettings = configSettings ?? throw new ArgumentNullException(nameof(configSettings));
        }

        public async Task Dashboard1ClaimsReport(Dashboard1ClaimsReportMessage arg)
        {
            _logger.LogInformation("The Dashboard1ClaimsReport started");

            TimeZoneInfo clientTimezone = null;
            if (!string.IsNullOrWhiteSpace(arg.Timezone))
            {
                try
                {
                    clientTimezone = _timeZoneAdapter.GetTimeZoneInfo(arg.Timezone);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Couldn't find the timezone {arg.Timezone} so that UTC will be used instead.");
                }
            }

            var pathToSaveTemp = Path.Combine(_configSettings.TemporaryFilesConfig.PathToSave, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
            if (!_fsa.DirectoryExists(pathToSaveTemp)) _fsa.CreateDirectoryRecursively(pathToSaveTemp);
            var baseUrlTemp = $"{_configSettings.TemporaryFilesConfig.BaseUrl}{DateTime.UtcNow.ToString(@"/yyyy/MM")}";
            var filename = $"{DateTime.UtcNow.Ticks}.csv";
            var csvFilename = Path.Combine(pathToSaveTemp, filename);
            var csvUrl = $"{baseUrlTemp}/{filename}";

            var pagingSettings = new PagingSettings
            {
                PageNumber = 1,
                PageSize = _configSettings.PageSize
            };
            double pageCount = 1;
            bool isReportSuccessfullyGenerated = true;

            using (var transientContext = _serviceProvider.GetRequiredService<InsuranceReportsDbContext>())
			{
                var query = GetClaimsQuery(arg, transientContext);

                do
                {
                    var page = await EfRepository<vw_dashboard1_claims_report>.PaginateAsync(pagingSettings, query).ConfigureAwait(false);
                    if (page.Results.Any())
                    {
                        var data = from r in page.Results
                                   select new Dashboard1ClaimsReportData
                                   {
                                       external_order_number = r.external_order_number,
                                       insurance_company_name = r.insurance_company_name,
                                       store_name = $"{r.store_sap_number}-{r.store_name}", // Here are two fields merged in 1 column
                                       claim_status_name = r.claim_status_name,
                                       claim_type_name = r.claim_type_name,
                                       claim_number = r.claim_number,
                                       policy_number = r.policy_number,
                                       policy_subsection = r.policy_subsection,
                                       report_number = r.report_number,
                                       items_quantity = r.items_quantity,
                                       date_created = clientTimezone != null ? TimeZoneInfo.ConvertTimeFromUtc(r.date_created, clientTimezone).ToString(DefaultDateTimeFormat) : r.date_created.ToString(DefaultDateTimeFormat),
                                       signature_date_created = r.signature_date_created.HasValue ? 
                                            clientTimezone != null ? 
                                                TimeZoneInfo.ConvertTimeFromUtc(r.signature_date_created.Value, clientTimezone).ToString(DefaultDateTimeFormat)
                                                : r.signature_date_created.Value.ToString(DefaultDateTimeFormat)
                                            : null
                                   };
                        try
                        {
                            _csvAdapter.Write(data, csvFilename);
                            _logger.LogInformation($"Dashboard1ClaimsReport: Page {pagingSettings.PageNumber} of {page.PageCount} completed");
                        }
                        catch (Exception ex)
                        {
                            isReportSuccessfullyGenerated = false;
                            _logger.LogError(ex, "Couldn't write to the CSV file so that the operation has been cancelled.");
                            return; // TODO: Verify this line is working as expected
                        }

                        // Move on to the next page
                        pagingSettings.PageNumber++;
                        pageCount = page.PageCount;
                    }
                    else
                    {
                        isReportSuccessfullyGenerated = false;
                        _logger.LogInformation("No records found for the given filters in Dashboard1ClaimsReport");
                        break; // TODO: Verify this line is working as expected
                    }
                } while (pagingSettings.PageNumber <= pageCount);
            }

            _logger.LogInformation("The Dashboard1ClaimsReport finished");

            if (isReportSuccessfullyGenerated)
            {
                // SignalR
                var message = new MessageDashboard1ClaimsReportReady
                {
                    Title = "CSV Generado",
                    Content = @$"Reporte de Siniestros",
                    Url = csvUrl,
                    HasUrl = true
                };
                var notification = new Notification
                {
                    Source = nameof(ReportJob),
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
        }

        private IQueryable<vw_dashboard1_claims_report> GetClaimsQuery(Dashboard1ClaimsReportMessage filters, InsuranceReportsDbContext transientContext)
        {
            var repository = new EfRepository<vw_dashboard1_claims_report>(transientContext);
            var query = from x in repository.TableNoTracking select x;

            // Filter by Insurance Company
            if (filters.DefaultInsuranceCompanyId != null && filters.DefaultInsuranceCompanyId.Any())
            {
                query = query.Where(x => filters.DefaultInsuranceCompanyId.Contains(x.insurance_company_id));
            }
            else if (filters.InsuranceCompanyId.HasValue && filters.InsuranceCompanyId.Value != 0)
            {
                query = query.Where(x => x.insurance_company_id == filters.InsuranceCompanyId);
            }
            // Filter by Store
            if (!string.IsNullOrWhiteSpace(filters.StoreName))
            {
                query = query.Where(x => (x.store_sap_number+"-"+x.store_name).Contains(filters.StoreName, StringComparison.OrdinalIgnoreCase));
            }
            if (filters.DefaultStoreId != null && filters.DefaultStoreId.Any())
            {
                query = query.Where(x => filters.DefaultStoreId.Contains(x.store_id));
            }
            // Filter by Keyword
            if (!string.IsNullOrWhiteSpace(filters.Keywords))
            {
                // Notice the keywords are converted to lowercase. Also there's no need to apply RegexOptions.IgnoreCase.
                // This is because the search will be performed against an EF Model.
                // See InsuranceCompanyService.GetAllClaims(... for an example of a different implementation.
                var keywords = filters.Keywords.EscapeRegexSpecialChars().ToLower().Split(' ');
                var regex = new Regex(string.Join("|", keywords));
                var regexString = regex.ToString();

                query = query.Where(x => 1 == 1 &&
                            (Regex.IsMatch(x.insurance_company_name, regexString) ||
                            Regex.IsMatch(x.store_name, regexString) || 
                            Regex.IsMatch(x.policy_number, regexString) ||
                            Regex.IsMatch(x.report_number, regexString) ||
                            Regex.IsMatch(x.claim_number, regexString) ||
                            Regex.IsMatch(x.external_order_number, regexString) ||
                            Regex.IsMatch(x.customer_full_name, regexString)));
            }
            // Filter by Status
            if (filters.StatusId.HasValue && filters.StatusId.Value != (int)EnumClaimStatusId.Unknown)
            {
                query = query.Where(x => x.claim_status_id == filters.StatusId.Value);
            }
            // Filter by Service Type
            if (filters.ServiceTypeId.HasValue && filters.ServiceTypeId.Value != (int)EnumClaimTypeId.Unknown)
            {
                query = query.Where(x => x.claim_type_id == filters.ServiceTypeId);
            }
            // Filter by Start Date
            if (filters.StartDate.HasValue)
            {
                query = query.Where(x => x.date_created >= filters.StartDate);
            }
            // Filter by End Date
            if (filters.EndDate.HasValue)
            {
                var endDate = filters.EndDate.Value.AddDays(1);
                query = query.Where(x => x.date_created < endDate);
            }

            // Order by date_created desc
            query = query.OrderBy(o => o.date_created);

            return query;
        }

        #region Local Classes

        public class ConfigSettings
        {
            public int PageSize { get; set; }
            public TemporaryFilesConfig TemporaryFilesConfig { get; set; }
        }

        public class TemporaryFilesConfig
        {
            public string PathToSave { get; set; }
            public string BaseUrl { get; set; }
        }

        // TODO: See all identifier options (they are interesting)
        // https://joshclose.github.io/CsvHelper/examples/configuration/attributes
        public class Dashboard1ClaimsReportData
        {
            [Name("No. AGRI")]
            public string external_order_number { get; set; }

            [Name("No. Siniestro")]
            public string claim_number { get; set; }

            [Name("Aseguradora")]
            public string insurance_company_name { get; set; }

            [Name("Taller")]
            public string store_name { get; set; }

            [Name("Status")]
            public string claim_status_name { get; set; }

            [Name("Tipo")]
            public string claim_type_name { get; set; }

            [Name("No. Poliza")]
            public string policy_number { get; set; }

            [Name("Inciso")]
            public string policy_subsection { get; set; }

            [Name("No. Reporte")]
            public string report_number { get; set; }

            [Name("Cristales")]
            public int items_quantity { get; set; }

            [Name("Fecha creacion")]
            public string date_created { get; set; }

            [Name("Fecha expediente completo")]
            public string signature_date_created { get; set; }
        }

        #endregion
    }
}
