namespace Aide.Hangfire.Common.Messages
{
	public class NewUserMessage
	{
		public string FirstName { get; set; }
		public string EmailAddress { get; set; }
		public string TemporaryPsw { get; set; }
	}
}
