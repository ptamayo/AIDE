using Aide.Claims.Domain.Objects;

namespace Aide.Claims.Domain.Extensions
{
	public static class StoreExtensions
	{
		public static ClaimStore ToClaimStore(this Store x)
		{
			var claimStore = new ClaimStore
			{
				Id = x.Id,
				Name = x.Name,
				Email = x.Email
			};
			if (!string.IsNullOrWhiteSpace(x.SAPNumber))
			{
				claimStore.Name = $"{x.SAPNumber.Trim()}-{claimStore.Name}";
			}
			return claimStore;
		}
	}
}
