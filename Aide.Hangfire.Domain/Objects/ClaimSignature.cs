using System;

namespace Aide.Hangfire.Domain.Objects
{
	public class ClaimSignature
	{
		public int Id { get; set; }
		public int ClaimId { get; set; }
		public string Signature { get; set; }
		public DateTime LocalDate { get; set; }
		public string LocalTimeZone { get; set; }
		public DateTime DateCreated { get; set; }
	}
}
