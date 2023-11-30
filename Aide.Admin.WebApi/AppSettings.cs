using System.Collections.Generic;

namespace Aide.Admin.WebApi
{
	public class AppSettings
	{
		public string License { get; set; }
		public CORSConfig CORSConfig { get; set; }
		public AuthenticationConfig AuthenticationConfig { get; set; }
		public CacheConfig CacheConfig { get; set; }
		public Impersonation Impersonation { get; set; }
		public SecurityLockConfig SecurityLockConfig { get; set; }
		public ServiceBusConfig ServiceBusConfig { get; set; }
		public ExternalServicesConfig[] ExternalServicesConfig { get; set; }
	}

	public class CORSConfig
	{
		public string[] Origins { get; set; }
	}

	public class AuthenticationConfig
	{
		public string SymmetricSecurityKey { get; set; }
	}

	public class CacheConfig
	{
		public bool Enabled { get; set; }
	}

	public class SecurityLockConfig
	{
		public bool IsEnabled { get; set; }
		public int MaximumAttempts { get; set; }
		public int LockLength { get; set; }
		public int TimeFrame { get; set; }
	}

	public class Impersonation
	{
		public bool Enabled { get; set; }
		public string Username { get; set; }
		public string Domain { get; set; }
		public string Password { get; set; }
	}

	public class ServiceBusConfig
	{
        public string ConnectionString { get; set; }
        public Dictionary<string, string> Queue { get; set; }
	}

	public class ExternalServicesConfig
	{
		public string Service { get; set; }
		public string BaseAddress { get; set; }
		public Dictionary<string, string> Endpoints { get; set; }
	}
}
