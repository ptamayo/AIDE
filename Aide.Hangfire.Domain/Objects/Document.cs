using System;

namespace Aide.Hangfire.Domain.Objects
{
	public class Document
	{
		public int Id { get; set; }
		public string MimeType { get; set; }
		public string Filename { get; set; }
		public string Url { get; set; }
		public string MetadataTitle { get; set; }
		public string MetadataAlt { get; set; }
		public string MetadataCopyright { get; set; }
		public string ChecksumSha1 { get; set; }
		public string ChecksumMd5 { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateModified { get; set; }
	}
}
