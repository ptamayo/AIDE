namespace Aide.Hangfire.Common.Messages
{
	public class PdfExportClaimFilesMessage
	{
		public int ClaimId { get; set; }
		public int ClaimDocumentTypeId { get; set; }
		public int ClaimDocumentSortPriority { get; set; }
		public int ClaimDocumentGroupId { get; set; }
	}
}
