using System;
using System.Collections.Generic;

namespace Aide.Admin.Models;

public partial class user_psw_history
{
    public int user_psw_history_id { get; set; }

    public int user_id { get; set; }

    public string psw { get; set; }

    public DateTime date_created { get; set; }
}
