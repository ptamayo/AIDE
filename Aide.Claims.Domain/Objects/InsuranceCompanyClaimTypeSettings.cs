using Aide.Claims.Domain.Enumerations;
using System;

namespace Aide.Claims.Domain.Objects
{
    public class InsuranceCompanyClaimTypeSettings
    {
        public int InsuranceCompanyId { get; set; }
        public EnumClaimTypeId ClaimTypeId { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsDepositSlipRequired { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
