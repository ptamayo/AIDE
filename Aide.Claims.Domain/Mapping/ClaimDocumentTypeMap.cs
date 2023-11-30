using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using System.Collections.Generic;

namespace Aide.Claims.Domain.Mapping
{
	public class ClaimDocumentTypeMap : Mapper
	{
		public static ClaimDocumentType ToDto(claim_document_type entity)
		{
			if (entity == null) return null;

			var dto = new ClaimDocumentType
			{
				Id = entity.claim_document_type_id,
				DocumentTypeId = entity.document_type_id,
				SortPriority = entity.sort_priority,
				GroupId = entity.group_id,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<ClaimDocumentType> ToDto(IEnumerable<claim_document_type> entities)
		{
			if (entities == null) return null;

			var dtos = new List<ClaimDocumentType>();
			foreach (var e in entities)
			{
				var dto = ClaimDocumentTypeMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static claim_document_type ToEntity(ClaimDocumentType dto)
		{
			if (dto == null) return null;

			var entity = new claim_document_type
			{
				document_type_id = dto.DocumentTypeId,
				sort_priority = dto.SortPriority,
				group_id = dto.GroupId,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static claim_document_type ToEntity(ClaimDocumentType dto, claim_document_type entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.document_type_id = dto.DocumentTypeId;
			e.sort_priority = dto.SortPriority;
			e.group_id = dto.GroupId;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static ClaimDocumentType ToDto(ClaimDocumentType sourceDto, ClaimDocumentType targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.DocumentTypeId = sourceDto.DocumentTypeId;
			targetDto.SortPriority = sourceDto.SortPriority;
			targetDto.GroupId = sourceDto.GroupId;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<ClaimDocumentType> ToDto(IPagedResult<claim_document_type> entityPage)
		{
			var dtos = new List<ClaimDocumentType>();
			foreach (var entity in entityPage.Results)
			{
				var dto = ClaimDocumentTypeMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<ClaimDocumentType>
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
