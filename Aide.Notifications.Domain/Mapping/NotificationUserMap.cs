using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Notifications.Domain.Objects;
using Aide.Notifications.Models;
using System.Collections.Generic;

namespace Aide.Notifications.Domain.Mapping
{
	public class NotificationUserMap : Mapper
	{
		public static NotificationUser ToDto(notification_user entity)
		{
			if (entity == null) return null;

			var dto = new NotificationUser
			{
				Id = entity.notification_user_id,
				NotificationId = entity.notification_id,
				UserId = entity.user_id,
				DateCreated = entity.date_created
			};

			return dto;
		}

		public static IEnumerable<NotificationUser> ToDto(IEnumerable<notification_user> entities)
		{
			if (entities == null) return null;

			var dtos = new List<NotificationUser>();
			foreach (var e in entities)
			{
				var dto = NotificationUserMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static notification_user ToEntity(NotificationUser dto)
		{
			if (dto == null) return null;

			var entity = new notification_user
			{
				notification_id = dto.NotificationId,
				user_id = dto.UserId,
				date_created = dto.DateCreated
			};

			return entity;
		}

		public static notification_user ToEntity(NotificationUser dto, notification_user entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.notification_id = dto.NotificationId;
			e.user_id = dto.UserId;
			e.date_created = dto.DateCreated;

			return entity;
		}

		public static NotificationUser ToDto(NotificationUser sourceDto, NotificationUser targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.NotificationId = sourceDto.NotificationId;
			targetDto.UserId = sourceDto.UserId;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<NotificationUser> ToDto(IPagedResult<notification_user> entityPage)
		{
			var dtos = new List<NotificationUser>();
			foreach (var entity in entityPage.Results)
			{
				var dto = NotificationUserMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<NotificationUser>
			{
				Results = dtos,
				CurrentPage = entityPage.CurrentPage,
				PageSize = entityPage.PageSize,
				PageCount = entityPage.PageCount,
				RowCount = entityPage.RowCount
			};
			return pageResult;
		}
	}
}
