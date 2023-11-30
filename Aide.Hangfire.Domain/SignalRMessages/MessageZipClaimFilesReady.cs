namespace Aide.Hangfire.Domain.SignalRMessages
{
	public class MessageZipClaimFilesReady : NotificationMessageBase
	{
		public int ClaimId { get; set; }
		public int DocumentId { get; set; }
	}
}
