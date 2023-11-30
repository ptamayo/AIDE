using System;
using System.Collections.Generic;

namespace Aide.Notifications.Models;

public partial class notification
{
    public int notification_id { get; set; }

    public int notification_type { get; set; }

    public string source { get; set; }

    public string target { get; set; }

    public string message_type { get; set; }

    public string message { get; set; }

    public DateTime date_created { get; set; }
}
