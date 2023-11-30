using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Enumerations;
using Aide.Claims.Models;
using System.Collections.Generic;
using ClaimOrder = Aide.Claims.Domain.Objects.Claim;

namespace Aide.Claims.Domain.Mapping
{
	public class ClaimMap : Mapper
	{
		public static ClaimOrder ToDto(claim entity)
		{
			if (entity == null) return null;

			var dto = new ClaimOrder
			{
				Id = entity.claim_id,
				ClaimStatusId = (EnumClaimStatusId)entity.claim_status_id,
				ClaimTypeId = (EnumClaimTypeId)entity.claim_type_id,
				CustomerFullName = entity.customer_full_name,
				PolicyNumber = entity.policy_number,
				PolicySubsection = entity.policy_subsection,
				ClaimNumber = entity.claim_number,
				ReportNumber = entity.report_number,
				ExternalOrderNumber = entity.external_order_number,
				InsuranceCompanyId = entity.insurance_company_id,
				StoreId = entity.store_id,
				ClaimProbatoryDocumentStatusId = (EnumClaimProbatoryDocumentStatusId)entity.claim_probatory_document_status_id,
				IsDepositSlipRequired = entity.is_deposit_slip_required,
				HasDepositSlip = entity.has_deposit_slip,
				ItemsQuantity = entity.items_quantity,
				Source = entity.source,
				CreatedByUserId = entity.created_by_user_id,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<ClaimOrder> ToDto(IEnumerable<claim> entities)
		{
			if (entities == null) return null;

			var dtos = new List<ClaimOrder>();
			foreach (var e in entities)
			{
				var dto = ClaimMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static claim ToEntity(ClaimOrder dto)
		{
			if (dto == null) return null;

			var entity = new claim
			{
				claim_status_id = (int)dto.ClaimStatusId,
				claim_type_id = (int)dto.ClaimTypeId,
				customer_full_name = dto.CustomerFullName,
				policy_number = dto.PolicyNumber,
				policy_subsection = dto.PolicySubsection,
				claim_number = dto.ClaimNumber,
				report_number = dto.ReportNumber,
				external_order_number = dto.ExternalOrderNumber,
				insurance_company_id = dto.InsuranceCompanyId,
				store_id = dto.StoreId,
				claim_probatory_document_status_id = (int)dto.ClaimProbatoryDocumentStatusId,
				is_deposit_slip_required = dto.IsDepositSlipRequired,
				has_deposit_slip = dto.HasDepositSlip,
				items_quantity = dto.ItemsQuantity,
				source = dto.Source,
				created_by_user_id = dto.CreatedByUserId,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static claim ToEntity(ClaimOrder dto, claim entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.claim_status_id = (int)dto.ClaimStatusId;
			e.claim_type_id = (int)dto.ClaimTypeId;
			e.customer_full_name = dto.CustomerFullName;
			e.policy_number = dto.PolicyNumber;
			e.policy_subsection = dto.PolicySubsection;
			e.claim_number = dto.ClaimNumber;
			e.report_number = dto.ReportNumber;
			e.external_order_number = dto.ExternalOrderNumber;
			e.insurance_company_id = dto.InsuranceCompanyId;
			e.store_id = dto.StoreId;
			e.claim_probatory_document_status_id = (int)dto.ClaimProbatoryDocumentStatusId;
			e.is_deposit_slip_required = dto.IsDepositSlipRequired;
			e.has_deposit_slip = dto.HasDepositSlip;
			e.items_quantity = dto.ItemsQuantity;
			e.source = dto.Source;
			e.created_by_user_id = dto.CreatedByUserId;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static ClaimOrder ToDto(ClaimOrder sourceDto, ClaimOrder targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.ClaimStatusId = sourceDto.ClaimStatusId;
			targetDto.ClaimTypeId = sourceDto.ClaimTypeId;
			targetDto.CustomerFullName = sourceDto.CustomerFullName;
			targetDto.PolicyNumber = sourceDto.PolicyNumber;
			targetDto.PolicySubsection = sourceDto.PolicySubsection;
			targetDto.ClaimNumber = sourceDto.ClaimNumber;
			targetDto.ReportNumber = sourceDto.ReportNumber;
			targetDto.ExternalOrderNumber = sourceDto.ExternalOrderNumber;
			targetDto.InsuranceCompanyId = sourceDto.InsuranceCompanyId;
			targetDto.StoreId = sourceDto.StoreId;
			targetDto.ClaimProbatoryDocumentStatusId = sourceDto.ClaimProbatoryDocumentStatusId;
			targetDto.IsDepositSlipRequired = sourceDto.IsDepositSlipRequired;
			targetDto.HasDepositSlip = sourceDto.HasDepositSlip;
			targetDto.ItemsQuantity = sourceDto.ItemsQuantity;
			targetDto.Source = sourceDto.Source;
			targetDto.CreatedByUserId = sourceDto.CreatedByUserId;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<ClaimOrder> ToDto(IPagedResult<claim> entityPage)
		{
			var dtos = new List<ClaimOrder>();
			foreach (var entity in entityPage.Results)
			{
				var dto = ClaimMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<ClaimOrder>
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
