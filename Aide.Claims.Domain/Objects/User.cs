using Aide.Claims.Domain.Enumerations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Aide.Claims.Domain.Objects
{
	public class User
	{
		public int Id { get; set; }
		public EnumUserRoleId RoleId { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }

		[JsonIgnore]
		public string Psw { get; set; }

		public IEnumerable<UserCompany> Companies { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateModified { get; set; }
		public DateTime DateLogout { get; set; }
	}
}
