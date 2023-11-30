using System;

namespace Aide.Admin.Domain.Objects
{
    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SAPNumber { get; set; }
        public string Email { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
