using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Enumerations;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using System.Collections.Generic;

namespace Aide.Claims.Domain.Mapping
{
	public class ClaimDocumentMap : Mapper
	{
		public static ClaimDocument ToDto(claim_document entity)
		{
			if (entity == null) return null;

			var dto = new ClaimDocument
			{
				Id = entity.claim_document_id,
				DocumentTypeId = entity.claim_document_type_id,
				StatusId = (EnumClaimDocumentStatusId)entity.claim_document_status_id,
				ClaimId = entity.claim_id,
				DocumentId = entity.document_id,
				SortPriority = entity.sort_priority,
				GroupId = entity.group_id,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<ClaimDocument> ToDto(IEnumerable<claim_document> entities)
		{
			if (entities == null) return null;

			var dtos = new List<ClaimDocument>();
			foreach (var e in entities)
			{
				var dto = ClaimDocumentMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static claim_document ToEntity(ClaimDocument dto)
		{
			if (dto == null) return null;

			var entity = new claim_document
			{
				claim_document_type_id = dto.DocumentTypeId,
				claim_document_status_id = (int)dto.StatusId,
				claim_id = dto.ClaimId,
				document_id = dto.DocumentId,
				sort_priority = dto.SortPriority,
				group_id = dto.GroupId,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static claim_document ToEntity(ClaimDocument dto, claim_document entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.claim_document_type_id = dto.DocumentTypeId;
			e.claim_document_status_id = (int)dto.StatusId;
			e.claim_id = dto.ClaimId;
			e.document_id = dto.DocumentId;
			e.sort_priority = dto.SortPriority;
			e.group_id = dto.GroupId;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static ClaimDocument ToDto(ClaimDocument sourceDto, ClaimDocument targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.DocumentTypeId = sourceDto.DocumentTypeId;
			targetDto.StatusId = sourceDto.StatusId;
			targetDto.ClaimId = sourceDto.ClaimId;
			targetDto.DocumentId = sourceDto.DocumentId;
			targetDto.SortPriority = sourceDto.SortPriority;
			targetDto.GroupId = sourceDto.GroupId;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<ClaimDocument> ToDto(IPagedResult<claim_document> entityPage)
		{
			var dtos = new List<ClaimDocument>();
			foreach (var entity in entityPage.Results)
			{
				var dto = ClaimDocumentMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<ClaimDocument>
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
