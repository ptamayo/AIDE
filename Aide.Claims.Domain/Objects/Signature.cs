using System;

namespace Aide.Claims.Domain.Objects
{
	public class Signature
	{
		public string Base64image { get; set; }
		public string LocalDate { get; set; }
		public string LocalTimeZone { get; set; }
		public DateTime DateCreated { get; set; }
	}
}
