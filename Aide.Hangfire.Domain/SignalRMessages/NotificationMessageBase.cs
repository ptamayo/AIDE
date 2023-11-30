namespace Aide.Hangfire.Domain.SignalRMessages
{
	public class NotificationMessageBase
	{
		public string Title { get; set; }
		public string Content { get; set; }
		public string Url { get; set; }
		public bool HasUrl { get; set; }
		public bool HasClaim { get; set; }
		public bool HasDepositSlip { get; set; }
	}
}
