using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using System.Collections.Generic;

namespace Aide.Admin.Domain.Mapping
{
	public class InsuranceCollageMap : Mapper
	{
		public static InsuranceCollage ToDto(insurance_collage entity)
		{
			if (entity == null) return null;

			var dto = new InsuranceCollage
			{
				Id = entity.insurance_collage_id,
				Name = entity.insurance_collage_name,
				InsuranceCompanyId = entity.insurance_company_id,
				ClaimTypeId = (EnumClaimTypeId)entity.claim_type_id,
				Columns = entity.columns,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<InsuranceCollage> ToDto(IEnumerable<insurance_collage> entities)
		{
			if (entities == null) return null;

			var dtos = new List<InsuranceCollage>();
			foreach (var e in entities)
			{
				var dto = InsuranceCollageMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static insurance_collage ToEntity(InsuranceCollage dto)
		{
			if (dto == null) return null;

			var entity = new insurance_collage
			{
				insurance_collage_name = dto.Name,
				insurance_company_id = dto.InsuranceCompanyId,
				claim_type_id = (int)dto.ClaimTypeId,
				columns = dto.Columns,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static insurance_collage ToEntity(InsuranceCollage dto, insurance_collage entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.insurance_collage_name = dto.Name;
			e.insurance_company_id = dto.InsuranceCompanyId;
			e.claim_type_id = (int)dto.ClaimTypeId;
			e.columns = dto.Columns;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static InsuranceCollage ToDto(InsuranceCollage sourceDto, InsuranceCollage targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.Name = sourceDto.Name;
			targetDto.InsuranceCompanyId = sourceDto.InsuranceCompanyId;
			targetDto.ClaimTypeId = sourceDto.ClaimTypeId;
			targetDto.Columns = sourceDto.Columns;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<InsuranceCollage> ToDto(IPagedResult<InsuranceCollage> dtoPage)
		{
			var dtos = new List<InsuranceCollage>();
			foreach (var dto in dtoPage.Results)
			{
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<InsuranceCollage>
			{
				Results = dtos,
				CurrentPage = dtoPage.CurrentPage,
				PageSize = dtoPage.PageSize,
				PageCount = dtoPage.PageCount,
				RowCount = dtoPage.RowCount
			};
			return pageResult;
		}

		public static IEnumerable<InsuranceCollage> Clone(IEnumerable<InsuranceCollage> sourceDto)
		{
			if (sourceDto == null) return null;

			var targetDto = new List<InsuranceCollage>();
			foreach (var dto in sourceDto)
			{
				var cloneDto = ToDto(dto, new InsuranceCollage());
				cloneDto.Id = dto.Id;
				cloneDto.Name = dto.Name;
				cloneDto.InsuranceCompanyId = dto.InsuranceCompanyId;
				cloneDto.ClaimTypeId = dto.ClaimTypeId;
				cloneDto.ClaimType = dto.ClaimType;
				cloneDto.Columns = dto.Columns;
				cloneDto.ProbatoryDocuments = InsuranceCollageProbatoryDocumentMap.Clone(dto.ProbatoryDocuments);
				cloneDto.DateCreated = dto.DateCreated;
				cloneDto.DateModified = dto.DateModified;
				targetDto.Add(cloneDto);
			}

			return targetDto;
		}
	}
}
