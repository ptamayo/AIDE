using Aide.Core.Data;
using Aide.Admin.Models;
using System.Collections.Generic;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Objects;

namespace Aide.Admin.Domain.Mapping
{
    public class UserPswHistoryMap : Mapper
    {
		public static UserPswHistory ToDto(user_psw_history entity)
		{
			if (entity == null) return null;

			var dto = new UserPswHistory
			{
				Id = entity.user_psw_history_id,
				UserId = entity.user_id,
				Psw = entity.psw,
				DateCreated = entity.date_created
			};

			return dto;
		}

		public static IEnumerable<UserPswHistory> ToDto(IEnumerable<user_psw_history> entities)
		{
			if (entities == null) return null;

			var dtos = new List<UserPswHistory>();
			foreach (var e in entities)
			{
				var dto = UserPswHistoryMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static user_psw_history ToEntity(UserPswHistory dto)
		{
			if (dto == null) return null;

			var entity = new user_psw_history
			{
				user_id = dto.UserId,
				psw = dto.Psw,
				date_created = dto.DateCreated
			};

			return entity;
		}

		public static user_psw_history ToEntity(UserPswHistory dto, user_psw_history entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.user_id = dto.UserId;
			e.psw = dto.Psw;
			e.date_created = dto.DateCreated;

			return e;
		}

		public static UserPswHistory ToDto(UserPswHistory sourceDto, UserPswHistory targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.UserId = sourceDto.UserId;
			targetDto.Psw = sourceDto.Psw;

			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;

			return targetDto;
		}

		public static IPagedResult<UserPswHistory> ToDto(IPagedResult<user_psw_history> entityPage)
		{
			var dtos = new List<UserPswHistory>();
			foreach (var entity in entityPage.Results)
			{
				var dto = UserPswHistoryMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<UserPswHistory>
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
