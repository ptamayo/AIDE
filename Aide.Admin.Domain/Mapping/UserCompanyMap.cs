using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using System.Collections.Generic;

namespace Aide.Admin.Domain.Mapping
{
	public class UserCompanyMap
	{
		public static UserCompany ToDto(user_company entity)
		{
			if (entity == null) return null;

			var dto = new UserCompany
			{
				Id = entity.user_company_id,
				UserId = entity.user_id,
				CompanyId = entity.company_id,
				CompanyTypeId = (EnumCompanyTypeId)entity.company_type_id,
				DateCreated = entity.date_created
			};

			return dto;
		}

		public static IEnumerable<UserCompany> ToDto(IEnumerable<user_company> entities)
		{
			if (entities == null) return null;

			var dtos = new List<UserCompany>();
			foreach (var e in entities)
			{
				var dto = UserCompanyMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static user_company ToEntity(UserCompany dto)
		{
			if (dto == null) return null;

			var entity = new user_company
			{
				user_id = dto.UserId,
				company_id = dto.CompanyId,
				company_type_id = (int)dto.CompanyTypeId,
				date_created = dto.DateCreated
			};

			return entity;
		}
	}
}
