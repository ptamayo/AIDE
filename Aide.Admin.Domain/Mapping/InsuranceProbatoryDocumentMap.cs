using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aide.Admin.Domain.Mapping
{
	public class InsuranceProbatoryDocumentMap : Mapper
	{
		public static InsuranceProbatoryDocument ToDto(insurance_probatory_document entity)
		{
			if (entity == null) return null;

			var dto = new InsuranceProbatoryDocument
			{
				Id = entity.insurance_probatory_document_id,
				InsuranceCompanyId = entity.insurance_company_id,
				ClaimTypeId = (EnumClaimTypeId)entity.claim_type_id,
				ProbatoryDocumentId = entity.probatory_document_id,
				SortPriority = entity.sort_priority,
				GroupId = entity.group_id,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<InsuranceProbatoryDocument> ToDto(IEnumerable<insurance_probatory_document> entities)
		{
			if (entities == null) return null;

			var dtos = new List<InsuranceProbatoryDocument>();
			foreach (var e in entities)
			{
				var dto = InsuranceProbatoryDocumentMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static insurance_probatory_document ToEntity(InsuranceProbatoryDocument dto)
		{
			if (dto == null) return null;

			var entity = new insurance_probatory_document
			{
				insurance_company_id = dto.InsuranceCompanyId,
				claim_type_id = (int)dto.ClaimTypeId,
				probatory_document_id = dto.ProbatoryDocumentId,
				sort_priority = dto.SortPriority,
				group_id = dto.GroupId,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static insurance_probatory_document ToEntity(InsuranceProbatoryDocument dto, insurance_probatory_document entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.insurance_company_id = dto.InsuranceCompanyId;
			e.claim_type_id = (int)dto.ClaimTypeId;
			e.probatory_document_id = dto.ProbatoryDocumentId;
			e.sort_priority = dto.SortPriority;
			e.group_id = dto.GroupId;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static InsuranceProbatoryDocument ToDto(InsuranceProbatoryDocument sourceDto, InsuranceProbatoryDocument targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.InsuranceCompanyId = sourceDto.InsuranceCompanyId;
			targetDto.ClaimTypeId = sourceDto.ClaimTypeId;
			targetDto.ProbatoryDocumentId = sourceDto.ProbatoryDocumentId;
			targetDto.ProbatoryDocument = sourceDto.ProbatoryDocument;
			targetDto.SortPriority = sourceDto.SortPriority;
			targetDto.GroupId = sourceDto.GroupId;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<InsuranceProbatoryDocument> ToDto(IPagedResult<insurance_probatory_document> entityPage)
		{
			var dtos = new List<InsuranceProbatoryDocument>();
			foreach (var entity in entityPage.Results)
			{
				var dto = InsuranceProbatoryDocumentMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<InsuranceProbatoryDocument>
			{
				Results = dtos,
				CurrentPage = entityPage.CurrentPage,
				PageSize = entityPage.PageSize,
				PageCount = entityPage.PageCount,
				RowCount = entityPage.RowCount
			};
			return pageResult;
		}

		public static InsuranceProbatoryDocument Clone(InsuranceProbatoryDocument sourceDto)
		{
			if (sourceDto == null) return null;

			var targetDto = new InsuranceProbatoryDocument
			{
				Id = sourceDto.Id,
				InsuranceCompanyId = sourceDto.InsuranceCompanyId,
				ClaimTypeId = sourceDto.ClaimTypeId,
				ProbatoryDocumentId = sourceDto.ProbatoryDocumentId,
				ProbatoryDocument = new ProbatoryDocument { 
					Id = sourceDto.ProbatoryDocument.Id,
					Name = sourceDto.ProbatoryDocument.Name,
					DateCreated = sourceDto.ProbatoryDocument.DateCreated,
					DateModified = sourceDto.ProbatoryDocument.DateModified
				},
				SortPriority = sourceDto.SortPriority,
				GroupId = sourceDto.GroupId,
				DateCreated = sourceDto.DateCreated,
				DateModified = sourceDto.DateModified
		};

			return targetDto;
		}
	}
}
