using System;
using System.Collections.Generic;

namespace Aide.Claims.Models;

public partial class claim
{
    public int claim_id { get; set; }

    public int claim_status_id { get; set; }

    public int claim_type_id { get; set; }

    public string customer_full_name { get; set; }

    public string policy_number { get; set; }

    public string policy_subsection { get; set; }

    public string claim_number { get; set; }

    public string report_number { get; set; }

    public string external_order_number { get; set; }

    public int insurance_company_id { get; set; }

    public int store_id { get; set; }

    public int claim_probatory_document_status_id { get; set; }

    public bool is_deposit_slip_required { get; set; }

    public bool has_deposit_slip { get; set; }

    public int items_quantity { get; set; }

    public string source { get; set; }

    public int created_by_user_id { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
