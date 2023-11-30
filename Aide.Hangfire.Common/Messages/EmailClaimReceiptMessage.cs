namespace Aide.Hangfire.Common.Messages
{
	public class EmailClaimReceiptMessage
	{
		public int ClaimId { get; set; }
		public string ExternalOrderNumber { get; set; }
		public string CustomerFullName { get; set; }
		public string InsuranceCompanyName { get; set; }
		public string StoreName { get; set; }
		public string StoreEmail { get; set; }
		public string[] ClaimProbatoryDocuments { get; set; }
		public int ClaimProbatoryDocumentId { get; set; }
	}
}
