using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Aide.Core.Data;
using System.Collections.Generic;

namespace Aide.Admin.Domain.Mapping
{
    public class InsuranceCompanyClaimTypeSettingsMap : Mapper
    {
		public static InsuranceCompanyClaimTypeSettings ToDto(insurance_company_claim_type_settings entity)
		{
			if (entity == null) return null;

			var dto = new InsuranceCompanyClaimTypeSettings
			{
				InsuranceCompanyId = entity.insurance_company_id,
				ClaimTypeId = (EnumClaimTypeId)entity.claim_type_id,
				IsClaimServiceEnabled = entity.is_enabled,
				IsDepositSlipRequired = entity.is_deposit_slip_required,
				IsExportingCustomizedDocsToPdf = entity.is_exporting_customized_docs_to_pdf,
				IsExportingCustomizedDocsToZip = entity.is_exporting_customized_docs_to_zip,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<InsuranceCompanyClaimTypeSettings> ToDto(IEnumerable<insurance_company_claim_type_settings> entities)
		{
			if (entities == null) return null;

			var dtos = new List<InsuranceCompanyClaimTypeSettings>();
			foreach (var e in entities)
			{
				var dto = InsuranceCompanyClaimTypeSettingsMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static insurance_company_claim_type_settings ToEntity(InsuranceCompanyClaimTypeSettings dto)
		{
			if (dto == null) return null;

			var entity = new insurance_company_claim_type_settings
			{
				insurance_company_id = dto.InsuranceCompanyId,
				claim_type_id = (int)dto.ClaimTypeId,
				is_enabled = dto.IsClaimServiceEnabled.HasValue ? dto.IsClaimServiceEnabled.Value : false,
				is_deposit_slip_required = dto.IsDepositSlipRequired.HasValue ? dto.IsDepositSlipRequired.Value : false,
				is_exporting_customized_docs_to_pdf = dto.IsExportingCustomizedDocsToPdf.HasValue ? dto.IsExportingCustomizedDocsToPdf.Value : false,
				is_exporting_customized_docs_to_zip = dto.IsExportingCustomizedDocsToZip.HasValue ? dto.IsExportingCustomizedDocsToZip.Value : false,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static insurance_company_claim_type_settings ToEntity(InsuranceCompanyClaimTypeSettings dto, insurance_company_claim_type_settings entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.insurance_company_id = dto.InsuranceCompanyId;
			e.claim_type_id = (int)dto.ClaimTypeId;
			e.is_enabled = dto.IsClaimServiceEnabled.HasValue ? dto.IsClaimServiceEnabled.Value : e.is_enabled;
			e.is_deposit_slip_required = dto.IsDepositSlipRequired.HasValue ? dto.IsDepositSlipRequired.Value : e.is_deposit_slip_required;
			e.is_exporting_customized_docs_to_pdf = dto.IsExportingCustomizedDocsToPdf.HasValue ? dto.IsExportingCustomizedDocsToPdf.Value : e.is_exporting_customized_docs_to_pdf;
			e.is_exporting_customized_docs_to_zip = dto.IsExportingCustomizedDocsToZip.HasValue ? dto.IsExportingCustomizedDocsToZip.Value : e.is_exporting_customized_docs_to_zip;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static InsuranceCompanyClaimTypeSettings ToDto(InsuranceCompanyClaimTypeSettings sourceDto, InsuranceCompanyClaimTypeSettings targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.InsuranceCompanyId = sourceDto.InsuranceCompanyId;
			targetDto.ClaimTypeId = sourceDto.ClaimTypeId;
			targetDto.IsClaimServiceEnabled = sourceDto.IsClaimServiceEnabled.HasValue ? sourceDto.IsClaimServiceEnabled : targetDto.IsClaimServiceEnabled;
			targetDto.IsDepositSlipRequired = sourceDto.IsDepositSlipRequired.HasValue ? sourceDto.IsDepositSlipRequired : targetDto.IsDepositSlipRequired;
			targetDto.IsExportingCustomizedDocsToPdf = sourceDto.IsExportingCustomizedDocsToPdf.HasValue ? sourceDto.IsExportingCustomizedDocsToPdf : targetDto.IsExportingCustomizedDocsToPdf;
			targetDto.IsExportingCustomizedDocsToZip = sourceDto.IsExportingCustomizedDocsToZip.HasValue ? sourceDto.IsExportingCustomizedDocsToZip : targetDto.IsExportingCustomizedDocsToZip;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}
	}
}
