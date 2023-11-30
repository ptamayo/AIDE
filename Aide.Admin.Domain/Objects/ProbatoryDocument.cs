﻿using Aide.Admin.Domain.Enumerations;
using System;

namespace Aide.Admin.Domain.Objects
{
    public class ProbatoryDocument
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public EnumDocumentOrientationId Orientation { get; set; }
        public string AcceptedFileExtensions { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
