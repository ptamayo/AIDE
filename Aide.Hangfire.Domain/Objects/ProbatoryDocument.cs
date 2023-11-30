using Aide.Hangfire.Domain.Enumerations;
using System;

namespace Aide.Hangfire.Domain.Objects
{
    public class ProbatoryDocument
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public EnumDocumentOrientationId Orientation { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
