using System;
using System.Collections.Generic;

namespace Aide.Claims.Models;

public partial class claim_document_type
{
    public int claim_document_type_id { get; set; }

    public int document_type_id { get; set; }

    public int sort_priority { get; set; }

    public int group_id { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
