using System;
using System.Collections.Generic;

namespace Aide.Reports.Models;

public partial class vw_dashboard1_claims_report
{
    public int insurance_company_id { get; set; }

    public string insurance_company_name { get; set; }

    public int store_id { get; set; }

    public string store_sap_number { get; set; }

    public string store_name { get; set; }

    public int claim_status_id { get; set; }

    public string claim_status_name { get; set; }

    public int claim_type_id { get; set; }

    public string claim_type_name { get; set; }

    public int claim_id { get; set; }

    public string external_order_number { get; set; }

    public string claim_number { get; set; }

    public string policy_number { get; set; }

    public string policy_subsection { get; set; }

    public string report_number { get; set; }

    public int items_quantity { get; set; }

    public string customer_full_name { get; set; }

    public DateTime date_created { get; set; }

    public DateTime? signature_date_created { get; set; }
}
