using Aide.Claims.Domain.Objects;

namespace Aide.Claims.Domain.Extensions
{
	public static class UserExtensions
	{
		public static ClaimCreatedByUser ToClaimCreatedByUser(this User x)
		{
			return new ClaimCreatedByUser
			{
				FirstName = x.FirstName,
				LastName = x.LastName,
				Email = x.Email
			};
		}
	}
}
