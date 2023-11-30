using System;

namespace Aide.Admin.Domain.Objects
{
	public class InsuranceCollageProbatoryDocument
	{
		public int Id { get; set; }
		public int InsuranceCollageId { get; set; }
		public int ProbatoryDocumentId { get; set; }
		public int SortPriority { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateModified { get; set; }
	}
}
