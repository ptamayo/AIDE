using Aide.Claims.Domain.Enumerations;
using Aide.Claims.Domain.Objects;

namespace Aide.Claims.Domain.Extensions
{
	public static class ClaimDocumentTypeExtensions
	{
		public static ClaimDocument ToClaimDocument(this ClaimDocumentType x)
		{
			return new ClaimDocument
			{
				DocumentTypeId = x.DocumentTypeId,
				DocumentType = x.DocumentType,
				SortPriority = x.SortPriority,
				GroupId = x.GroupId,
				StatusId = EnumClaimDocumentStatusId.Pending
			};
		}
	}
}
