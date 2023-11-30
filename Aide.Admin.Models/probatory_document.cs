using System;
using System.Collections.Generic;

namespace Aide.Admin.Models;

public partial class probatory_document
{
    public int probatory_document_id { get; set; }

    public string probatory_document_name { get; set; }

    public int probatory_document_orientation { get; set; }

    public string accepted_file_extensions { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
