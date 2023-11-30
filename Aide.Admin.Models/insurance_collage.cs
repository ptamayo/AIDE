using System;
using System.Collections.Generic;

namespace Aide.Admin.Models;

public partial class insurance_collage
{
    public int insurance_collage_id { get; set; }

    public string insurance_collage_name { get; set; }

    public int insurance_company_id { get; set; }

    public int claim_type_id { get; set; }

    public int columns { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
