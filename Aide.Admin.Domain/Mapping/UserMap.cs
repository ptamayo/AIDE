using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using System.Collections.Generic;
using System.Linq;

namespace Aide.Admin.Domain.Mapping
{
	public class UserMap : Mapper
	{
		public static User ToDto(user entity)
		{
			if (entity == null) return null;

			var dto = new User
			{
				Id = entity.user_id,
				RoleId = (EnumUserRoleId)entity.role_id,
				FirstName = entity.user_first_name,
				LastName = entity.user_last_name,
				Email = entity.email,
				Psw = entity.psw,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified,
				DateLogout = entity.date_logout,
				LastLoginAttempt = entity.last_login_attempt,
				TimeLastAttempt = entity.time_last_attempt
			};

			return dto;
		}

		public static IEnumerable<User> ToDto(IEnumerable<user> entities)
		{
			if (entities == null) return null;

			var dtos = new List<User>();
			foreach (var e in entities)
			{
				var dto = UserMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static user ToEntity(User dto)
		{
			if (dto == null) return null;

			var entity = new user
			{
				role_id = (int)dto.RoleId,
				user_first_name = dto.FirstName,
				user_last_name = dto.LastName,
				email = dto.Email,
				psw = dto.Psw,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
				date_logout = dto.DateLogout,
				last_login_attempt = dto.LastLoginAttempt,
				time_last_attempt = dto.TimeLastAttempt
			};

			return entity;
		}

		public static user ToEntity(User dto, user entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.role_id = (int)dto.RoleId;
			e.user_first_name = dto.FirstName;
			e.user_last_name = dto.LastName;
			e.email = dto.Email;
			e.psw = dto.Psw;
			e.date_modified = dto.DateModified;
			e.last_login_attempt = dto.LastLoginAttempt;
			e.time_last_attempt	= dto.TimeLastAttempt;

			// These lines are commented because we don't want accidentally overwrite the dates when updating an user
			//e.date_created = dto.DateCreated;
			//e.date_logout = dto.DateLogout;

			return e; 
		}

		public static User ToDto(User sourceDto, User targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.RoleId = sourceDto.RoleId;
			targetDto.FirstName = sourceDto.FirstName;
			targetDto.LastName = sourceDto.LastName;
			targetDto.Email = sourceDto.Email;
			targetDto.Psw = sourceDto.Psw;
			if (sourceDto.Companies != null)
			{
				targetDto.Companies = sourceDto.Companies.ToList(); // IMPORTANT: Here the list is cloned
			}
			else
			{
				targetDto.Companies = new List<UserCompany>();
			}
			targetDto.DateLogout = sourceDto.DateLogout;
			targetDto.LastLoginAttempt = sourceDto.LastLoginAttempt;
			targetDto.TimeLastAttempt = sourceDto.TimeLastAttempt;

			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<User> ToDto(IPagedResult<user> entityPage)
		{
			var dtos = new List<User>();
			foreach (var entity in entityPage.Results)
			{
				var dto = UserMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<User>
			{
				Results = dtos,
				CurrentPage = entityPage.CurrentPage,
				PageSize = entityPage.PageSize,
				PageCount = entityPage.PageCount,
				RowCount = entityPage.RowCount
			};
			return pageResult;
		}

		public static IEnumerable<User> Clone(IEnumerable<User> sourceDto)
		{
			if (sourceDto == null) return null;

			var targetDto = new List<User>();
			foreach(var dto in sourceDto)
			{
				var cloneDto = ToDto(dto, new User());
				cloneDto.Id = dto.Id;
				cloneDto.DateCreated = dto.DateCreated;
				cloneDto.DateModified = dto.DateModified;

				cloneDto.LastLoginAttempt = dto.LastLoginAttempt;
				cloneDto.TimeLastAttempt = dto.TimeLastAttempt;

				targetDto.Add(cloneDto);
			}

			return targetDto;
		}
	}
}
