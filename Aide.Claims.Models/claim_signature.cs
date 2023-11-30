using System;
using System.Collections.Generic;

namespace Aide.Claims.Models;

public partial class claim_signature
{
    public int claim_signature_id { get; set; }

    public int claim_id { get; set; }

    public string signature { get; set; }

    public DateTime local_date { get; set; }

    public string local_timezone { get; set; }

    public DateTime date_created { get; set; }
}
