using System;

namespace Aide.Claims.Domain.Objects
{
    public class ClaimProbatoryDocument
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public int? ClaimItemId { get; set; }
        public int ProbatoryDocumentId { get; set; }
        public int SortPriority { get; set; }
        public int GroupId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }

        public ProbatoryDocument ProbatoryDocument { get; set; }
        public Media Media { get; set; }
    }
}
