using System;
using System.Collections.Generic;

namespace Aide.Claims.Models;

public partial class document
{
    public int document_id { get; set; }

    public string mime_type { get; set; }

    public string filename { get; set; }

    public string url { get; set; }

    public string metadata_title { get; set; }

    public string metadata_alt { get; set; }

    public string metadata_copyright { get; set; }

    public string checksum_sha1 { get; set; }

    public string checksum_md5 { get; set; }

    public DateTime date_created { get; set; }

    public DateTime date_modified { get; set; }
}
