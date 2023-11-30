using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using System.Collections.Generic;

namespace Aide.Admin.Domain.Mapping
{
	public class StoreMap : Mapper
	{
		public static Store ToDto(store entity)
		{
			if (entity == null) return null;

			var dto = new Store
			{
				Id = entity.store_id,
				Name = entity.store_name,
				SAPNumber = entity.store_sap_number,
				Email = entity.store_email,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<Store> ToDto(IEnumerable<store> entities)
		{
			if (entities == null) return null;

			var dtos = new List<Store>();
			foreach (var e in entities)
			{
				var dto = StoreMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static store ToEntity(Store dto)
		{
			if (dto == null) return null;

			var entity = new store
			{
				store_name = dto.Name,
				store_sap_number = dto.SAPNumber,
				store_email = dto.Email,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static store ToEntity(Store dto, store entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.store_name = dto.Name;
			e.store_sap_number = dto.SAPNumber;
			e.store_email = dto.Email;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static Store ToDto(Store sourceDto, Store targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.Name = sourceDto.Name;
			targetDto.SAPNumber = sourceDto.SAPNumber;
			targetDto.Email = sourceDto.Email;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<Store> ToDto(IPagedResult<store> entityPage)
		{
			var dtos = new List<Store>();
			foreach (var entity in entityPage.Results)
			{
				var dto = StoreMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<Store>
			{
				Results = dtos,
				CurrentPage = entityPage.CurrentPage,
				PageSize = entityPage.PageSize,
				PageCount = entityPage.PageCount,
				RowCount = entityPage.RowCount
			};
			return pageResult;
		}

		public static IPagedResult<Store> ToDto(IPagedResult<Store> dtoPage)
		{
			var dtos = new List<Store>();
			foreach (var dto in dtoPage.Results)
			{
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<Store>
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
