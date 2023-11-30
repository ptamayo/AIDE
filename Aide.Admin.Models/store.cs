using System;
using System.Collections.Generic;

namespace Aide.Admin.Models;

public partial class store
{
    public int store_id { get; set; }

    public string store_name { get; set; }

    public string store_sap_number { get; set; }

    public string store_email { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
