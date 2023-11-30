using System;
using System.Collections.Generic;

namespace Aide.Claims.Models;

public partial class claim_type
{
    public int claim_type_id { get; set; }

    public string claim_type_name { get; set; }

    public int sort_priority { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
