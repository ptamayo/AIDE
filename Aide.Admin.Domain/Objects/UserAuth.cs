namespace Aide.Admin.Domain.Objects
{
	public class UserAuth
	{
		public string Token { get; set; }
		public bool IsLoginSuccessful { get; set; }
		public string Message { get; set; }
		public bool IsUserLocked { get; set; }
	}
}
