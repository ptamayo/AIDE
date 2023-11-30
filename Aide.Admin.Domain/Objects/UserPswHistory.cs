using System;
using System.Collections.Generic;
using System.Text;

namespace Aide.Admin.Domain.Objects
{
    public class UserPswHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Psw { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
