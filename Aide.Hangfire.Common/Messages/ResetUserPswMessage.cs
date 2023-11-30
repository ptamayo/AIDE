namespace Aide.Hangfire.Common.Messages
{
	public class ResetUserPswMessage
	{
		public string FirstName { get; set; }
		public string EmailAddress { get; set; }
		public string TemporaryPsw { get; set; }
	}
}
