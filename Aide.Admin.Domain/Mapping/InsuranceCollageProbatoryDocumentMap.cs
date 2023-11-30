using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Aide.Core.Data;
using System.Collections.Generic;

namespace Aide.Admin.Domain.Mapping
{
	public class InsuranceCollageProbatoryDocumentMap : Mapper
	{
		public static InsuranceCollageProbatoryDocument ToDto(insurance_collage_probatory_document entity)
		{
			if (entity == null) return null;

			var dto = new InsuranceCollageProbatoryDocument
			{
				Id = entity.insurance_collage_probatory_document_id,
				InsuranceCollageId = entity.insurance_collage_id,
				ProbatoryDocumentId = entity.probatory_document_id,
				SortPriority = entity.sort_priority,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<InsuranceCollageProbatoryDocument> ToDto(IEnumerable<insurance_collage_probatory_document> entities)
		{
			if (entities == null) return null;

			var dtos = new List<InsuranceCollageProbatoryDocument>();
			foreach (var e in entities)
			{
				var dto = InsuranceCollageProbatoryDocumentMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static insurance_collage_probatory_document ToEntity(InsuranceCollageProbatoryDocument dto)
		{
			if (dto == null) return null;

			var entity = new insurance_collage_probatory_document
			{
				insurance_collage_id = dto.InsuranceCollageId,
				probatory_document_id = dto.ProbatoryDocumentId,
				sort_priority = dto.SortPriority,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified
			};

			return entity;
		}

		public static insurance_collage_probatory_document ToEntity(InsuranceCollageProbatoryDocument dto, insurance_collage_probatory_document entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.insurance_collage_id = dto.InsuranceCollageId;
			e.probatory_document_id = dto.ProbatoryDocumentId;
			e.sort_priority = dto.SortPriority;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static InsuranceCollageProbatoryDocument ToDto(InsuranceCollageProbatoryDocument sourceDto, InsuranceCollageProbatoryDocument targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.InsuranceCollageId = sourceDto.InsuranceCollageId;
			targetDto.ProbatoryDocumentId = sourceDto.ProbatoryDocumentId;
			targetDto.SortPriority = sourceDto.SortPriority;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IEnumerable<InsuranceCollageProbatoryDocument> Clone(IEnumerable<InsuranceCollageProbatoryDocument> sourceDto)
		{
			if (sourceDto == null) return null;

			var targetDto = new List<InsuranceCollageProbatoryDocument>();
			foreach (var dto in sourceDto)
			{
				var cloneDto = ToDto(dto, new InsuranceCollageProbatoryDocument());
				cloneDto.Id = dto.Id;
				cloneDto.InsuranceCollageId = dto.InsuranceCollageId;
				cloneDto.ProbatoryDocumentId = dto.ProbatoryDocumentId;
				cloneDto.SortPriority = dto.SortPriority;
				cloneDto.DateCreated = dto.DateCreated;
				cloneDto.DateModified = dto.DateModified;
				targetDto.Add(cloneDto);
			}

			return targetDto;
		}
	}
}
