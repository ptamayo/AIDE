using Aide.Hangfire.Domain.Enumerations;
using System;

namespace Aide.Hangfire.Domain.Objects
{
	public class Notification
	{
		public int Id { get; set; }
		public EnumNotificationTypeId Type { get; set; }
		public string Source { get; set; }
		public string Target { get; set; }
		public string MessageType { get; set; }
		public string Message { get; set; }
		public DateTime DateCreated { get; set; }

		public bool IsRead { get; set; }
	}
}
