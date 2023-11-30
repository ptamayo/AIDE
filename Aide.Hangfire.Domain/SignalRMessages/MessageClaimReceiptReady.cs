namespace Aide.Hangfire.Domain.SignalRMessages
{
	public class MessageClaimReceiptReady : NotificationMessageBase
	{
		public int ClaimId { get; set; }
		public int MediaId { get; set; }
	}
}
