using System;

namespace Aide.Claims.Domain.Objects
{
	public class DocumentType
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string AcceptedFileExtensions { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateModified { get; set; }
	}
}
