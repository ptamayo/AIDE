using System;

namespace Aide.Hangfire.Domain.Objects
{
	public class DocumentType
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateModified { get; set; }
	}
}
