using System;
using System.Collections.Generic;

namespace Aide.Admin.Models;

public partial class insurance_company_claim_type_settings
{
    public int insurance_company_id { get; set; }

    public int claim_type_id { get; set; }

    public bool is_enabled { get; set; }

    public bool is_deposit_slip_required { get; set; }

    public bool is_exporting_customized_docs_to_pdf { get; set; }

    public bool is_exporting_customized_docs_to_zip { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
