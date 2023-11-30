using Aide.Admin.Domain.Enumerations;
using System;

namespace Aide.Admin.Domain.Objects
{
    public class InsuranceExportProbatoryDocument
    {
        public int Id { get; set; }
        public EnumExportTypeId ExportTypeId { get; set; }
        public int InsuranceCompanyId { get; set; }
        public EnumClaimTypeId ClaimTypeId { get; set; }
        public EnumExportDocumentTypeId ExportDocumentTypeId { get; set; }
        public int SortPriority { get; set; }
        public int? ProbatoryDocumentId { get; set; }
        public int? CollageId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }

        public string Name { get; set; }
    }
}
