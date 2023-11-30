using System;

namespace Aide.Hangfire.Domain.Objects
{
    public class ClaimProbatoryDocumentMedia
	{
		public int Id { get; set; }
		public int ClaimProbatoryDocumentId { get; set; }
		public int MediaId { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateModified { get; set; }
	}
}
