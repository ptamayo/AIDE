using System;

namespace Aide.Notifications.Domain.Objects
{
	public class NotificationUser
	{
		public int Id { get; set; }
		public int NotificationId { get; set; }
		public int UserId { get; set; }
		public DateTime DateCreated { get; set; }
	}
}
