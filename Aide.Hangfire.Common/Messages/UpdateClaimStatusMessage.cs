namespace Aide.Hangfire.Common.Messages
{
	public class UpdateClaimStatusMessage
	{
		public int ClaimId { get; set; }
		public int ClaimStatusId { get; set; }
	}
}
