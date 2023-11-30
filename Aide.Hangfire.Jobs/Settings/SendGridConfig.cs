namespace Aide.Hangfire.Jobs.Settings
{
	public class SendGridConfig
	{
		public string ApiKey { get; set; }
		public string ClaimReceiptEmailTemplateId { get; set; }
		public bool ClaimReceiptEmailEnabled { get; set; }
        public string NewUserEmailTemplateId { get; set; }
        public bool NewUserEmailEnabled { get; set; }
        public string ResetUserPswEmailTemplateId { get; set; }
        public bool ResetUserPswEmailEnabled { get; set; }
    }
}
