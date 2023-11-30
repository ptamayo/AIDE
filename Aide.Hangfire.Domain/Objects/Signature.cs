using System;

namespace Aide.Hangfire.Domain.Objects
{
	public class Signature
	{
		public string Base64image { get; set; }
		public string LocalDate { get; set; }
		public string LocalTimeZone { get; set; }
		public DateTime DateCreated { get; set; }
	}
}
