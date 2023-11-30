namespace Aide.Notifications.WebApi
{
	public class AppSettings
	{
		public string License { get; set; }
		public CORSConfig CORSConfig { get; set; }
	}

	public class CORSConfig
	{
		public string[] Origins { get; set; }
	}
}
