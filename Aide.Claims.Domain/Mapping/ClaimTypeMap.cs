using System.Collections.Generic;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;

namespace Aide.Claims.Domain.Mapping
{
	public class ClaimTypeMap : Mapper
	{
		public static ClaimType ToDto(claim_type entity)
		{
			if (entity == null) return null;

			var dto = new ClaimType
			{
				Id = entity.claim_type_id,
				Name = entity.claim_type_name,
				SortPriority = entity.sort_priority,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<ClaimType> ToDto(IEnumerable<claim_type> entities)
		{
			if (entities == null) return null;

			var dtos = new List<ClaimType>();
			foreach (var e in entities)
			{
				var dto = ClaimTypeMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static claim_type ToEntity(ClaimType dto)
		{
			if (dto == null) return null;

			var entity = new claim_type
			{
				claim_type_name = dto.Name,
				sort_priority = dto.SortPriority,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static claim_type ToEntity(ClaimType dto, claim_type entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.claim_type_name = dto.Name;
			e.sort_priority = dto.SortPriority;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static ClaimType ToDto(ClaimType sourceDto, ClaimType targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.Name = sourceDto.Name;
			targetDto.SortPriority = sourceDto.SortPriority;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<ClaimType> ToDto(IPagedResult<claim_type> entityPage)
		{
			var dtos = new List<ClaimType>();
			foreach (var entity in entityPage.Results)
			{
				var dto = ClaimTypeMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<ClaimType>
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
