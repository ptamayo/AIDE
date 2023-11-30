using Aide.Admin.Domain.Enumerations;
using System;
using System.Collections.Generic;

namespace Aide.Admin.Domain.Objects
{
	public class InsuranceCollage
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int InsuranceCompanyId { get; set; }
		public EnumClaimTypeId ClaimTypeId { get; set; }
		public ClaimType ClaimType { get; set; }
		public int Columns { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateModified { get; set; }

		public IEnumerable<InsuranceCollageProbatoryDocument> ProbatoryDocuments { get; set; }
	}
}
