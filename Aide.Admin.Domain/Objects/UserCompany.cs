using Aide.Admin.Domain.Enumerations;
using System;

namespace Aide.Admin.Domain.Objects
{
	public class UserCompany
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public int CompanyId { get; set; }
		public EnumCompanyTypeId CompanyTypeId { get; set; }
		public DateTime DateCreated { get; set; }
	}
}
