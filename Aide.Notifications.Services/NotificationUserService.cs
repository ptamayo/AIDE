using Aide.Core.Data;
using Aide.Notifications.Domain.Mapping;
using Aide.Notifications.Domain.Objects;
using Aide.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aide.Notifications.Services
{
	public interface INotificationUserService
	{
		Task<IEnumerable<NotificationUser>> GetNotificationUserListByNotificationIdsAndUserId(int[] notificationIds, int userId);
		Task<double> GetCountOfReadNotifications(int userId);
		Task<NotificationUser> InsertNotificationUser(NotificationUser dto);
		Task<IEnumerable<NotificationUser>> InsertNotificationUser(List<NotificationUser> dtos);
	}

	public class NotificationUserService : INotificationUserService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;

		#endregion

		#region Constructor

		public NotificationUserService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<NotificationUser>> GetNotificationUserListByNotificationIdsAndUserId(int[] notificationIds, int userId)
		{
			if (notificationIds == null || !notificationIds.Any()) throw new ArgumentNullException(nameof(notificationIds));
			if (userId <= 0) throw new ArgumentException(nameof(userId));

			var dtos = new List<NotificationUser>();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<notification_user>(transientContext);
				var query = from notification_user in repository.TableNoTracking
							where notification_user.user_id == userId && notificationIds.Contains(notification_user.notification_id)
							select notification_user;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return null;

				foreach (var entity in entities)
				{
					var dto = NotificationUserMap.ToDto(entity);
					dtos.Add(dto);
				}
			}
			return dtos;
		}

		public async Task<double> GetCountOfReadNotifications(int userId)
		{
			if (userId <= 0) throw new ArgumentException(nameof(userId));
			double totalCount = 0;
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<notification_user>(transientContext);
				var query = from notification_user in repository.TableNoTracking
							where notification_user.user_id == userId
							select notification_user;
				totalCount = await query.CountAsync().ConfigureAwait(false);
			}
			return totalCount;
		}

		public async Task<NotificationUser> InsertNotificationUser(NotificationUser dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			dto.DateCreated = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<notification_user>(transientContext);

				// First verify the record does not exist
				//var whereExpression = $"user_id == {dto.UserId} && notification_id == {dto.NotificationId}";
				//var query = await repository.GetAllAsync(whereExpression).ConfigureAwait(false);
				//var count = query.Count();
				var query = from notification_user in repository.Table
							where notification_user.user_id == dto.UserId && notification_user.notification_id == dto.NotificationId
							select notification_user;
				var count = await query.CountAsync().ConfigureAwait(false);
				if (count == 0)
				{
					// Persist the read status on the notification
					var entity = NotificationUserMap.ToEntity(dto);
					await repository.InsertAsync(entity).ConfigureAwait(false);
					dto.Id = entity.notification_user_id;
				}
			}

			return dto;
		}

		public async Task<IEnumerable<NotificationUser>> InsertNotificationUser(List<NotificationUser> dtos)
		{
			if (dtos == null || !dtos.Any()) throw new ArgumentNullException();

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<notification_user>(transientContext);
				foreach (var dto in dtos)
				{
					// First verify the record does not exist
					//var whereExpression = $"user_id == {dto.UserId} && notification_id == {dto.NotificationId}";
					//var query = await repository.GetAllAsync(whereExpression).ConfigureAwait(false);
					//var count = query.Count();
					var query = from notification_user in repository.Table
								where notification_user.user_id == dto.UserId && notification_user.notification_id == dto.NotificationId
								select notification_user;
					var count = await query.CountAsync().ConfigureAwait(false);
					if (count == 0)
					{
						// Persist the read status on the notification
						dto.DateCreated = DateTime.UtcNow;
						var entity = NotificationUserMap.ToEntity(dto);
						await repository.InsertAsync(entity).ConfigureAwait(false);
						dto.Id = entity.notification_user_id;
					}
				}
			}

			return dtos;
		}

		#endregion

		#region Local classes

		public class Filters
		{
			public string Keywords { get; set; }
		}

		#endregion
	}
}
