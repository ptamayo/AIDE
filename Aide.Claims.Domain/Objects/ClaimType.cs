using System;

namespace Aide.Claims.Domain.Objects
{
    public class ClaimType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SortPriority { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
