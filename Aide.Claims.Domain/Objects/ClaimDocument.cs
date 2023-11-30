using Aide.Claims.Domain.Enumerations;
using System;

namespace Aide.Claims.Domain.Objects
{
	public class ClaimDocument
	{
		public int Id { get; set; }
		public int DocumentTypeId { get; set; }
		public EnumClaimDocumentStatusId StatusId { get; set; }
		public int ClaimId { get; set; }
		public int DocumentId { get; set; }
		public int SortPriority { get; set; }
		public int GroupId { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateModified { get; set; }

		public DocumentType DocumentType { get; set; }
		public Document Document { get; set; }
	}
}
