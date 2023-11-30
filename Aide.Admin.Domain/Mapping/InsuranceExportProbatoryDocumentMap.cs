using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using System.Collections.Generic;

namespace Aide.Admin.Domain.Mapping
{
    public class InsuranceExportProbatoryDocumentMap : Mapper
	{
		public static InsuranceExportProbatoryDocument ToDto(insurance_export_probatory_document entity)
		{
			if (entity == null) return null;

			var dto = new InsuranceExportProbatoryDocument
			{
				Id = entity.insurance_export_probatory_document_id,
				ExportTypeId = (EnumExportTypeId)entity.export_type_id,
				InsuranceCompanyId = entity.insurance_company_id,
				ClaimTypeId = (EnumClaimTypeId)entity.claim_type_id,
				ExportDocumentTypeId = (EnumExportDocumentTypeId)entity.export_document_type_id,
				SortPriority = entity.sort_priority,
				ProbatoryDocumentId = entity.probatory_document_id,
				CollageId = entity.collage_id,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<InsuranceExportProbatoryDocument> ToDto(IEnumerable<insurance_export_probatory_document> entities)
		{
			if (entities == null) return null;

			var dtos = new List<InsuranceExportProbatoryDocument>();
			foreach (var e in entities)
			{
				var dto = InsuranceExportProbatoryDocumentMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static insurance_export_probatory_document ToEntity(InsuranceExportProbatoryDocument dto)
		{
			if (dto == null) return null;

			var entity = new insurance_export_probatory_document
			{
				export_type_id = (int)dto.ExportTypeId,
				insurance_company_id = dto.InsuranceCompanyId,
				claim_type_id = (int)dto.ClaimTypeId,
				export_document_type_id = (int)dto.ExportDocumentTypeId,
				sort_priority = dto.SortPriority,
                probatory_document_id = dto.ProbatoryDocumentId,
				collage_id = dto.CollageId,
                date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static insurance_export_probatory_document ToEntity(InsuranceExportProbatoryDocument dto, insurance_export_probatory_document entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.export_type_id = (int)dto.ExportTypeId;
			e.insurance_company_id = dto.InsuranceCompanyId;
			e.claim_type_id = (int)dto.ClaimTypeId;
			e.export_document_type_id = (int)dto.ExportDocumentTypeId;
			e.sort_priority = dto.SortPriority;
            e.probatory_document_id = dto.ProbatoryDocumentId;
			e.collage_id = dto.CollageId;
            e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static InsuranceExportProbatoryDocument ToDto(InsuranceExportProbatoryDocument sourceDto, InsuranceExportProbatoryDocument targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.ExportTypeId = sourceDto.ExportTypeId;
			targetDto.InsuranceCompanyId = sourceDto.InsuranceCompanyId;
			targetDto.ClaimTypeId = sourceDto.ClaimTypeId;
			targetDto.ExportDocumentTypeId = sourceDto.ExportDocumentTypeId;
			targetDto.SortPriority = sourceDto.SortPriority;
			targetDto.ProbatoryDocumentId = sourceDto.ProbatoryDocumentId;
			targetDto.CollageId = sourceDto.CollageId;
            targetDto.Name = sourceDto.Name;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<InsuranceExportProbatoryDocument> ToDto(IPagedResult<insurance_export_probatory_document> entityPage)
		{
			var dtos = new List<InsuranceExportProbatoryDocument>();
			foreach (var entity in entityPage.Results)
			{
				var dto = InsuranceExportProbatoryDocumentMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<InsuranceExportProbatoryDocument>
			{
				Results = dtos,
				CurrentPage = entityPage.CurrentPage,
				PageSize = entityPage.PageSize,
				PageCount = entityPage.PageCount,
				RowCount = entityPage.RowCount
			};
			return pageResult;
		}

		public static InsuranceExportProbatoryDocument Clone(InsuranceExportProbatoryDocument sourceDto)
		{
			if (sourceDto == null) return null;

			var targetDto = new InsuranceExportProbatoryDocument
			{
				Id = sourceDto.Id,
				ExportTypeId = sourceDto.ExportTypeId,
				InsuranceCompanyId = sourceDto.InsuranceCompanyId,
				ClaimTypeId = sourceDto.ClaimTypeId,
				ExportDocumentTypeId = sourceDto.ExportDocumentTypeId,
                SortPriority = sourceDto.SortPriority,
				ProbatoryDocumentId = sourceDto.ProbatoryDocumentId,
				CollageId = sourceDto.CollageId,
				Name = sourceDto.Name,
				DateCreated = sourceDto.DateCreated,
				DateModified = sourceDto.DateModified
			};

			return targetDto;
		}
	}
}
