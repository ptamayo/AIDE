using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Notifications.Domain.Enumerations;
using Aide.Notifications.Domain.Objects;
using Aide.Notifications.Models;
using System.Collections.Generic;

namespace Aide.Notifications.Domain.Mapping
{
	public class NotificationMap : Mapper
	{
		public static Notification ToDto(notification entity)
		{
			if (entity == null) return null;

			var dto = new Notification
			{
				Id = entity.notification_id,
				Type = (EnumNotificationTypeId)entity.notification_type,
				Source = entity.source,
				Target = entity.target,
				MessageType = entity.message_type,
				Message = entity.message,
				DateCreated = entity.date_created
			};

			return dto;
		}

		public static IEnumerable<Notification> ToDto(IEnumerable<notification> entities)
		{
			if (entities == null) return null;

			var dtos = new List<Notification>();
			foreach (var e in entities)
			{
				var dto = NotificationMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static notification ToEntity(Notification dto)
		{
			if (dto == null) return null;

			var entity = new notification
			{
				notification_type = (int)dto.Type,
				source = dto.Source,
				target = dto.Target,
				message_type = dto.MessageType,
				message = dto.Message,
				date_created = dto.DateCreated,
			};

			return entity;
		}

		public static notification ToEntity(Notification dto, notification entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.notification_type = (int)dto.Type;
			e.source = dto.Source;
			e.target = dto.Target;
			e.message_type = dto.MessageType;
			e.message = dto.Message;
			e.date_created = dto.DateCreated;

			return entity;
		}

		public static Notification ToDto(Notification sourceDto, Notification targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.Type = sourceDto.Type;
			targetDto.Source = sourceDto.Source;
			targetDto.Target = sourceDto.Target;
			targetDto.MessageType = sourceDto.MessageType;
			targetDto.Message = sourceDto.Message;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<Notification> ToDto(IPagedResult<notification> entityPage)
		{
			var dtos = new List<Notification>();
			foreach (var entity in entityPage.Results)
			{
				var dto = NotificationMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<Notification>
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
