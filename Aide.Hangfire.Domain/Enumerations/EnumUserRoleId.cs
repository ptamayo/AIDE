using System.ComponentModel;

namespace Aide.Hangfire.Domain.Enumerations
{
	public enum EnumUserRoleId
	{
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
