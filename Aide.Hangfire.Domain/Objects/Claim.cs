using Aide.Hangfire.Domain.Enumerations;
using System;
using System.Collections.Generic;

namespace Aide.Hangfire.Domain.Objects
{
    public class Claim
    {
        public int Id { get; set; }
        public EnumClaimStatusId ClaimStatusId { get; set; }
        public EnumClaimTypeId ClaimTypeId { get; set; }
        public string CustomerFullName { get; set; }
        public string PolicyNumber { get; set; }
        public string PolicySubsection { get; set; }
        public string ClaimNumber { get; set; }
        public string ReportNumber { get; set; }
        public string ExternalOrderNumber { get; set; }
        public int InsuranceCompanyId { get; set; }
        public int StoreId { get; set; }
        public EnumClaimProbatoryDocumentStatusId ClaimProbatoryDocumentStatusId { get; set; }
        public bool IsDepositSlipRequired { get; set; }
        public bool HasDepositSlip { get; set; }
        public int ItemsQuantity { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }

        public ClaimCreatedByUser CreatedByUser { get; set; }
        public ClaimType ClaimType { get; set; }
        public ClaimStore Store { get; set; }
        public ClaimInsuranceCompany InsuranceCompany { get; set; }
        public IEnumerable<ClaimProbatoryDocument> ClaimProbatoryDocuments { get; set; }
        public IEnumerable<ClaimDocument> ClaimDocuments { get; set; }
    }
}
