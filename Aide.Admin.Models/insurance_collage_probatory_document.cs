using System;
using System.Collections.Generic;

namespace Aide.Admin.Models;

public partial class insurance_collage_probatory_document
{
    public int insurance_collage_probatory_document_id { get; set; }

    public int insurance_collage_id { get; set; }

    public int probatory_document_id { get; set; }

    public int sort_priority { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
