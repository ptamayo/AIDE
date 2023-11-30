using Aide.Hangfire.Domain.Enumerations;

namespace Aide.Hangfire.Domain.Objects
{
    public class ExportDocument
	{
		public EnumExportDocumentTypeId ExportDocumentTypeId { get; set; }
		public int SortPriority { get; set; }
		public int? ProbatoryDocumentId { get; set; }
		public int? CollageId { get; set; }
	}
}
