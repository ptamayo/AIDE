using Aide.Claims.Domain.Enumerations;
using System;

namespace Aide.Claims.Domain.Objects
{
    public class InsuranceProbatoryDocument
    {
        public int Id { get; set; }
        public int InsuranceCompanyId { get; set; }
        public EnumClaimTypeId ClaimTypeId { get; set; }
        public int ProbatoryDocumentId { get; set; }
        public int SortPriority { get; set; }
        public int GroupId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }

        public ProbatoryDocument ProbatoryDocument { get; set; }
    }
}
