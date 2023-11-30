using System;
using System.Collections.Generic;

namespace Aide.Hangfire.Common.Messages
{
	public class Dashboard1ClaimsReportMessage
	{
		public IEnumerable<int> DefaultInsuranceCompanyId { get; set; }
		public IEnumerable<int> DefaultStoreId { get; set; }
		public string Keywords { get; set; }
		public int? StatusId { get; set; }
		public string StoreName { get; set; }
		public int? ServiceTypeId { get; set; }
		public int? InsuranceCompanyId { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public string Timezone { get; set; }
	}
}
