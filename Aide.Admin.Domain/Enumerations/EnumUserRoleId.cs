using System.ComponentModel;

namespace Aide.Admin.Domain.Enumerations
{
	public enum EnumUserRoleId
	{
		[Description("System")]
		System = 0,

		[Description("Administrator")]
		Admin = 1,

		[Description("Insurance Read-only")]
		InsuranceReadOnly = 2,

		[Description("Workshop Administrator")]
		WsAdmin = 3,

		[Description("Workshop Operator")]
		WsOperator = 4
	}
}
