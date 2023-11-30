using Aide.Hangfire.Domain.Objects;

namespace Aide.Hangfire.Common.Messages
{
	public class UpdateClaimMessage
	{
		public ClaimServiceRequest ClaimUpdateRequest { get; set; }
	}
}
