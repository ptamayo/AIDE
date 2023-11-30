using System.Collections.Generic;

namespace Aide.Hangfire.Domain.Objects
{
    public class InsuranceExportSettings
	{
		public bool IsExportingCustomizedDocsOnly { get; set; }
		public IEnumerable<ExportDocument> ExportDocuments { get; set; }
	}
}
