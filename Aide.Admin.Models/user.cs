using System;
using System.Collections.Generic;

namespace Aide.Admin.Models;

public partial class user
{
    public int user_id { get; set; }

    public int role_id { get; set; }

    public string user_first_name { get; set; }

    public string user_last_name { get; set; }

    public string email { get; set; }

    public string psw { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }

    public DateTime date_logout { get; set; }

    public int? last_login_attempt { get; set; }

    public DateTime? time_last_attempt { get; set; }
}
