using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using System.Collections.Generic;

namespace Aide.Claims.Domain.Mapping
{
	public class ClaimProbatoryDocumentMediaMap : Mapper
	{
		public static ClaimProbatoryDocumentMedia ToDto(claim_probatory_document_media entity)
		{
			if (entity == null) return null;

			var dto = new ClaimProbatoryDocumentMedia
			{
				Id = entity.claim_probatory_document_media_id,
				ClaimProbatoryDocumentId = entity.claim_probatory_document_id,
				MediaId = entity.media_id,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<ClaimProbatoryDocumentMedia> ToDto(IEnumerable<claim_probatory_document_media> entities)
		{
			if (entities == null) return null;

			var dtos = new List<ClaimProbatoryDocumentMedia>();
			foreach (var e in entities)
			{
				var dto = ClaimProbatoryDocumentMediaMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static claim_probatory_document_media ToEntity(ClaimProbatoryDocumentMedia dto)
		{
			if (dto == null) return null;

			var entity = new claim_probatory_document_media
			{
				claim_probatory_document_id = dto.ClaimProbatoryDocumentId,
				media_id = dto.MediaId,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static claim_probatory_document_media ToEntity(ClaimProbatoryDocumentMedia dto, claim_probatory_document_media entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.claim_probatory_document_id = dto.ClaimProbatoryDocumentId;
			e.media_id = dto.MediaId;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static ClaimProbatoryDocumentMedia ToDto(ClaimProbatoryDocumentMedia sourceDto, ClaimProbatoryDocumentMedia targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.ClaimProbatoryDocumentId = sourceDto.ClaimProbatoryDocumentId;
			targetDto.MediaId = sourceDto.MediaId;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<ClaimProbatoryDocumentMedia> ToDto(IPagedResult<claim_probatory_document_media> entityPage)
		{
			var dtos = new List<ClaimProbatoryDocumentMedia>();
			foreach (var entity in entityPage.Results)
			{
				var dto = ClaimProbatoryDocumentMediaMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<ClaimProbatoryDocumentMedia>
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
