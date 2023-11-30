namespace Aide.Hangfire.Domain.SignalRMessages
{
	public class MessagePdfClaimFilesReady : NotificationMessageBase
	{
		public int ClaimId { get; set; }
		public int DocumentId { get; set; }
	}
}
