using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using System.Collections.Generic;

namespace Aide.Admin.Domain.Mapping
{
	public class InsuranceCompanyMap : Mapper
	{
		public static InsuranceCompany ToDto(insurance_company entity)
		{
			if (entity == null) return null;

			var dto = new InsuranceCompany
			{
				Id = entity.insurance_company_id,
				Name = entity.insurance_company_name,
				IsEnabled = entity.is_enabled,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<InsuranceCompany> ToDto(IEnumerable<insurance_company> entities)
		{
			if (entities == null) return null;

			var dtos = new List<InsuranceCompany>();
			foreach(var e in entities)
			{
				var dto = InsuranceCompanyMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static insurance_company ToEntity(InsuranceCompany dto)
		{
			if (dto == null) return null;

			var entity = new insurance_company
			{
				insurance_company_name = dto.Name,
				is_enabled = dto.IsEnabled,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static insurance_company ToEntity(InsuranceCompany dto, insurance_company entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.insurance_company_name = dto.Name;
			e.is_enabled = dto.IsEnabled;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static InsuranceCompany ToDto(InsuranceCompany sourceDto, InsuranceCompany targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.Name = sourceDto.Name;
			targetDto.IsEnabled = sourceDto.IsEnabled;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<InsuranceCompany> ToDto(IPagedResult<insurance_company> entityPage)
		{
			var dtos = new List<InsuranceCompany>();
			foreach (var entity in entityPage.Results)
			{
				var dto = InsuranceCompanyMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<InsuranceCompany>
			{
				Results = dtos,
				CurrentPage = entityPage.CurrentPage,
				PageSize = entityPage.PageSize,
				PageCount = entityPage.PageCount,
				RowCount = entityPage.RowCount
			};
			return pageResult;
		}

		public static IPagedResult<InsuranceCompany> ToDto(IPagedResult<InsuranceCompany> dtoPage)
		{
			var dtos = new List<InsuranceCompany>();
			foreach (var dto in dtoPage.Results)
			{
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<InsuranceCompany>
			{
				Results = dtos,
				CurrentPage = dtoPage.CurrentPage,
				PageSize = dtoPage.PageSize,
				PageCount = dtoPage.PageCount,
				RowCount = dtoPage.RowCount
			};
			return pageResult;
		}
	}
}
