using System;
using System.Collections.Generic;

namespace Aide.Notifications.Models;

public partial class notification_user
{
    public int notification_user_id { get; set; }

    public int notification_id { get; set; }

    public int user_id { get; set; }

    public DateTime date_created { get; set; }
}
