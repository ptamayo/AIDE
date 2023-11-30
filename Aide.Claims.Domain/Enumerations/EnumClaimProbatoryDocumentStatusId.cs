using System.ComponentModel;

namespace Aide.Claims.Domain.Enumerations
{
	public enum EnumClaimProbatoryDocumentStatusId
	{
		[Description("In Progress")]
		InProgress = 1,

		[Description("Pending Review")]
		PendigReview,

		[Description("Need Rework")]
		NeedRework,

		[Description("Completed")]
		Completed
	}
}
