using Aide.Claims.Domain.Objects;

namespace Aide.Claims.Domain.Extensions
{
	public static class InsuranceProbatoryDocumentExtensions
	{
		public static ClaimProbatoryDocument ToClaimProbatoryDocument(this InsuranceProbatoryDocument x, int claimId)
		{
			return new ClaimProbatoryDocument
			{
				ClaimId = claimId,
				ProbatoryDocumentId = x.ProbatoryDocumentId,
				ProbatoryDocument = x.ProbatoryDocument,
				SortPriority = x.SortPriority,
				GroupId = x.GroupId,
				// The lines below will be set by the service class during insertion to DB
				// targetDto.DateCreated = sourceDto.DateCreated;
				// targetDto.DateModified = sourceDto.DateModified;
			};
		}

		public static ClaimProbatoryDocument ToClaimItemProbatoryDocument(this InsuranceProbatoryDocument x, int claimId, int claimItemId)
        {
			var claimProbatoryDocument = ToClaimProbatoryDocument(x, claimId);
			claimProbatoryDocument.ClaimItemId = claimItemId;
			return claimProbatoryDocument;
		}
	}
}
