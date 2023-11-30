using System;
using System.Collections.Generic;

namespace Aide.Claims.Models;

public partial class document_type
{
    public int document_type_id { get; set; }

    public string document_type_name { get; set; }

    public string accepted_file_extensions { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
