using System.Collections.Generic;

namespace Aide.Claims.WebApi
{
	public class AppSettings
	{
		public string License { get; set; }
		public CORSConfig CORSConfig { get; set; }
		public AuthenticationConfig AuthenticationConfig { get; set; }
		public CacheConfig CacheConfig { get; set; }
		public DocumentFilesConfig DocumentFilesConfig { get; set; }
		public MediaFilesConfig MediaFilesConfig { get; set; }
		public Impersonation Impersonation { get; set; }
		public ServiceBusConfig ServiceBusConfig { get; set; }
		public ExternalServicesConfig[] ExternalServicesConfig { get; set; }
		public ReceiptDocumentConfig ReceiptDocumentConfig { get; set; }
		public ZipDocumentConfig ZipDocumentConfig { get; set; }
		public PdfDocumentConfig PdfDocumentConfig { get; set; }
		public DepositSlipConfig DepositSlipConfig { get; set; }
		public StaleOrdersRemovalConfig StaleOrdersRemovalConfig { get; set; }
    }

	public class CORSConfig
	{
		public string[] Origins { get; set; }
	}

	public class AuthenticationConfig
	{
		public string SymmetricSecurityKey { get; set; }
	}

	public class CacheConfig
	{
		public bool Enabled { get; set; }
	}

	public class DocumentFilesConfig
	{
		public string PathToSave { get; set; }
	}

	public class MediaFilesConfig
	{
		public string PathToSave { get; set; }
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

        public Dictionary<string, string> Queue { get; set; }
	}

	public class ExternalServicesConfig
	{
		public string Service { get; set; }
		public string BaseAddress { get; set; }
		public Dictionary<string, string> Endpoints { get; set; }
	}

	public class ReceiptDocumentConfig
	{
		public int ProbatoryDocumentId { get; set; }
	}

	public class ZipDocumentConfig
	{
		public int DocumentTypeId { get; set; }
		public int GroupId { get; set; }
		public int SortPriority { get; set; }
	}

	public class PdfDocumentConfig
	{
		public int DocumentTypeId { get; set; }
		public int GroupId { get; set; }
		public int SortPriority { get; set; }
	}

	public class DepositSlipConfig
	{
		public ProbatoryDocumentConfig TPAProbatoryDocument { get; set; }
		public ProbatoryDocumentConfig StoreProbatoryDocument { get; set; }
	}

    public class DocumentTypeConfig
	{
		public int DocumentTypeId { get; set; }
		public int GroupId { get; set; }
		public int SortPriority { get; set; }
	}

	public class ProbatoryDocumentConfig
	{
		public int ProbatoryDocumentId { get; set; }
		public int GroupId { get; set; }
		public int SortPriority { get; set; }
	}

    public class StaleOrdersRemovalConfig
    {
        public string[] SourceExclusions { get; set; }
    }
}
