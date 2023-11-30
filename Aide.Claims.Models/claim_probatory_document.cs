using System;
using System.Collections.Generic;

namespace Aide.Claims.Models;

public partial class claim_probatory_document
{
    public int claim_probatory_document_id { get; set; }

    public int claim_id { get; set; }

    public int? claim_item_id { get; set; }

    public int probatory_document_id { get; set; }

    public int sort_priority { get; set; }

    public int group_id { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
