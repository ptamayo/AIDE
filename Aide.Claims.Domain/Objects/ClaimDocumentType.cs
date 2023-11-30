using System;

namespace Aide.Claims.Domain.Objects
{
	public class ClaimDocumentType
	{
		public int Id { get; set; }
		public int DocumentTypeId { get; set; }
		public int SortPriority { get; set; }
		public int GroupId { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateModified { get; set; }

		public DocumentType DocumentType { get; set; }
	}
}
