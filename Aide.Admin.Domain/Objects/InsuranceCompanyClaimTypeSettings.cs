using Aide.Admin.Domain.Enumerations;
using System;

namespace Aide.Admin.Domain.Objects
{
    public class InsuranceCompanyClaimTypeSettings
    {
        public int InsuranceCompanyId { get; set; }
        public EnumClaimTypeId ClaimTypeId { get; set; }
        public bool? IsClaimServiceEnabled { get; set; }
        public bool? IsDepositSlipRequired { get; set; }
        public bool? IsExportingCustomizedDocsToPdf { get; set; }
        public bool? IsExportingCustomizedDocsToZip { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
