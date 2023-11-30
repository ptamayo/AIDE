using System;
using System.Collections.Generic;

namespace Aide.Admin.Models;

public partial class insurance_company
{
    public int insurance_company_id { get; set; }

    public string insurance_company_name { get; set; }

    public bool is_enabled { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
