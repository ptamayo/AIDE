using System;
using System.Collections.Generic;

namespace Aide.Admin.Models;

public partial class insurance_export_probatory_document
{
    public int insurance_export_probatory_document_id { get; set; }

    public int export_type_id { get; set; }

    public int insurance_company_id { get; set; }

    public int claim_type_id { get; set; }

    public int export_document_type_id { get; set; }

    public int sort_priority { get; set; }

    public int? probatory_document_id { get; set; }

    public int? collage_id { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
