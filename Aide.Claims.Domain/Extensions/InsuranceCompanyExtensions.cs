using Aide.Claims.Domain.Objects;

namespace Aide.Claims.Domain.Extensions
{
	public static class InsuranceCompanyExtensions
	{
		public static ClaimInsuranceCompany ToClaimInsuranceCompany(this InsuranceCompany x)
		{
			return new ClaimInsuranceCompany
			{
				Id = x.Id,
				Name = x.Name
			};
		}
	}
}
