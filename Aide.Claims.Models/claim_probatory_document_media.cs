using System;
using System.Collections.Generic;

namespace Aide.Claims.Models;

public partial class claim_probatory_document_media
{
    public int claim_probatory_document_media_id { get; set; }

    public int claim_probatory_document_id { get; set; }

    public int media_id { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
