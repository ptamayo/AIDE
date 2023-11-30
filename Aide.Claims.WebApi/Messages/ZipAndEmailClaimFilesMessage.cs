namespace Aide.Hangfire.Common.Messages
{
	public class ZipAndEmailClaimFilesMessage
	{
		public int ClaimId { get; set; }
		public string EmailTo { get; set; }
	}
}
