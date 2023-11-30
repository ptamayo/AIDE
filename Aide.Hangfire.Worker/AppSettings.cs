using System.Collections.Generic;

namespace Aide.Hangfire.Worker
{
	public class AppSettings
	{
		public string License { get; set; }
		public string UrlWeb { get; set; }
		public EmailService EmailServiceConfig { get; set; }
		public JobServerConfig JobServerConfig { get; set; }
		public CacheConfig CacheConfig { get; set; }
		public Impersonation Impersonation { get; set; }
		public ServiceBusConfig ServiceBusConfig { get; set; }
		public ExternalServicesConfig ExternalServicesConfig { get; set; }
		public SendGridConfig SendGridConfig { get; set; }
		public PdfFilesConfig PdfFilesConfig { get; set; }
		public ZipFilesConfig ZipFilesConfig { get; set; }
		public TemporaryFilesConfig TemporaryFilesConfig { get; set; }
		public ReceiptDocumentConfig ReceiptDocumentConfig { get; set; }
		public MediaEngineConfig MediaEngineConfig { get; set; }
		public ReportJobConfig ReportJobConfig { get; set; }
		public RecurringJobsConfig[] RecurringJobsConfig { get; set; }
	}

	public class EmailService
    {
        public bool IsEnabled { get; set; }
        public EmailAddresses EmailAddresses { get; set; }
	}

	public class EmailAddresses
	{
		public string EmailFrom { get; set; }
		public string PilkingtonTpaEmail { get; set; }
		public string EmailForSupport { get; set; }
	}

	public class JobServerConfig
    {
		public int WorkerCount { get; set; }
    }

	public class CacheConfig
	{
		public bool Enabled { get; set; }
	}

	public class Impersonation
	{
		public bool Enabled { get; set; }
		public string Username { get; set; }
		public string Domain { get; set; }
		public string Password { get; set; }
	}

	public class ServiceBusConfig
	{
        public string ConnectionString { get; set; }
        public Dictionary<string, string> QueueConsumer { get; set; }
		public Dictionary<string, ThirdPartySystemNotificationQueue> ThirdPartySystemNotifications { get; set; }
	}

	public class ThirdPartySystemNotificationQueue
	{
		public bool Enabled { get; set; }
		public string Queue { get; set; }
	}

	public class ExternalServicesConfig
	{
		public ExternalServiceCredentials Credentials { get; set; }
		public ExternalService[] Services { get; set; }
	}

	public class ExternalServiceCredentials
	{
		public string Username { get; set; }
		public string HashedPsw { get; set; }
	}

	public class ExternalService
	{
		public string Service { get; set; }
		public string BaseAddress { get; set; }
		public Dictionary<string, string> Endpoints { get; set; }
	}

	public class SendGridConfig
	{
		public string ApiKey { get; set; }
		public string ClaimReceiptEmailTemplateId { get; set; }
        public bool ClaimReceiptEmailEnabled { get; set; }
        public string NewUserEmailTemplateId { get; set; }
        public bool NewUserEmailEnabled { get; set; }
        public string ResetUserPswEmailTemplateId { get; set; }
        public bool ResetUserPswEmailEnabled { get; set; }
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

	public class TemporaryFilesConfig
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

	public class MediaEngineConfig
	{
		public int LimitMemoryPercentage { get; set; }
		public int CollagePdfDensity { get; set; }
		public int CollageImageQuality { get; set; }
		public int CollageImageWidth { get; set; }
		public int ResizeImageWidth { get; set; }
		public string GhostscriptDirectory { get; set; }
	}

	public class ReportJobConfig
    {
		public int DefaultPageSize { get; set; }
    }

	public class RecurringJobsConfig
    {
		public string JobName { get; set; }
		public string CronExpression { get; set; }
		public string TimeZone { get; set; }
		public Dictionary<string, string> Args { get; set; }
    }
}
