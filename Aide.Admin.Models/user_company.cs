using System;
using System.Collections.Generic;

namespace Aide.Admin.Models;

public partial class user_company
{
    public int user_company_id { get; set; }

    public int user_id { get; set; }

    public int company_id { get; set; }

    public int company_type_id { get; set; }

    public DateTime date_created { get; set; }
}
