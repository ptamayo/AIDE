using Aide.Hangfire.Domain.Enumerations;
using System;
using System.Collections.Generic;

namespace Aide.Hangfire.Domain.Objects
{
	public class InsuranceCollage
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int InsuranceCompanyId { get; set; }
		public EnumClaimTypeId ClaimTypeId { get; set; }
		public int Columns { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateModified { get; set; }

		public IEnumerable<InsuranceCollageProbatoryDocument> ProbatoryDocuments { get; set; }
		public Media Media { get; set; } // ???
	}
}
