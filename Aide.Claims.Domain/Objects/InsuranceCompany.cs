using System;
using System.Collections.Generic;

namespace Aide.Claims.Domain.Objects
{
    public class InsuranceCompany
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }

        public IDictionary<int, InsuranceCompanyClaimTypeSettings> ClaimTypeSettings { get; set; }
    }
}
