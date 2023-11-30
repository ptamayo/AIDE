using Aide.Hangfire.Domain.Objects;

namespace Aide.Hangfire.Common.Messages
{
	public class InsertClaimMessage
	{
		public ClaimServiceRequest ClaimInsertRequest { get; set; }
	}
}
