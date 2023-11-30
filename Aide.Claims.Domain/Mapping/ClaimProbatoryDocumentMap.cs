using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using System.Collections.Generic;

namespace Aide.Claims.Domain.Mapping
{
	public class ClaimProbatoryDocumentMap : Mapper
	{
		public static ClaimProbatoryDocument ToDto(claim_probatory_document entity)
		{
			if (entity == null) return null;

			var dto = new ClaimProbatoryDocument
			{
				Id = entity.claim_probatory_document_id,
				ClaimId = entity.claim_id,
				ClaimItemId = entity.claim_item_id,
				ProbatoryDocumentId = entity.probatory_document_id,
				SortPriority = entity.sort_priority,
				GroupId = entity.group_id,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<ClaimProbatoryDocument> ToDto(IEnumerable<claim_probatory_document> entities)
		{
			if (entities == null) return null;

			var dtos = new List<ClaimProbatoryDocument>();
			foreach (var e in entities)
			{
				var dto = ClaimProbatoryDocumentMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static claim_probatory_document ToEntity(ClaimProbatoryDocument dto)
		{
			if (dto == null) return null;

			var entity = new claim_probatory_document
			{
				claim_id = dto.ClaimId,
				claim_item_id = dto.ClaimItemId,
				probatory_document_id = dto.ProbatoryDocumentId,
				sort_priority = dto.SortPriority,
				group_id = dto.GroupId,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static claim_probatory_document ToEntity(ClaimProbatoryDocument dto, claim_probatory_document entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.claim_id = dto.ClaimId;
			e.claim_item_id = dto.ClaimItemId;
			e.probatory_document_id = dto.ProbatoryDocumentId;
			e.sort_priority = dto.SortPriority;
			e.group_id = dto.GroupId;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static ClaimProbatoryDocument ToDto(ClaimProbatoryDocument sourceDto, ClaimProbatoryDocument targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.ClaimId = sourceDto.ClaimId;
			targetDto.ClaimItemId = sourceDto.ClaimItemId;
			targetDto.ProbatoryDocumentId = sourceDto.ProbatoryDocumentId;
			targetDto.ProbatoryDocument = sourceDto.ProbatoryDocument;
			targetDto.SortPriority = sourceDto.SortPriority;
			targetDto.GroupId = sourceDto.GroupId;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<ClaimProbatoryDocument> ToDto(IPagedResult<claim_probatory_document> entityPage)
		{
			var dtos = new List<ClaimProbatoryDocument>();
			foreach (var entity in entityPage.Results)
			{
				var dto = ClaimProbatoryDocumentMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<ClaimProbatoryDocument>
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
