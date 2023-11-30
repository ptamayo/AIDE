using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using System.Collections.Generic;

namespace Aide.Claims.Domain.Mapping
{
	public class ClaimSignatureMap : Mapper
	{
		public static ClaimSignature ToDto(claim_signature entity)
		{
			if (entity == null) return null;

			var dto = new ClaimSignature
			{
				Id = entity.claim_signature_id,
				ClaimId = entity.claim_id,
				Signature = entity.signature,
				LocalDate = entity.local_date,
				LocalTimeZone = entity.local_timezone,
				DateCreated = entity.date_created
			};

			return dto;
		}

		public static IEnumerable<ClaimSignature> ToDto(IEnumerable<claim_signature> entities)
		{
			if (entities == null) return null;

			var dtos = new List<ClaimSignature>();
			foreach (var e in entities)
			{
				var dto = ClaimSignatureMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static claim_signature ToEntity(ClaimSignature dto)
		{
			if (dto == null) return null;

			var entity = new claim_signature
			{
				claim_id = dto.ClaimId,
				signature = dto.Signature,
				local_date = dto.LocalDate,
				local_timezone = dto.LocalTimeZone,
				date_created = dto.DateCreated
			};

			return entity;
		}

		public static claim_signature ToEntity(ClaimSignature dto, claim_signature entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.claim_id = dto.ClaimId;
			e.signature = dto.Signature;
			e.local_date = dto.LocalDate;
			e.local_timezone = dto.LocalTimeZone;
			e.date_created = dto.DateCreated;

			return entity;
		}

		public static ClaimSignature ToDto(ClaimSignature sourceDto, ClaimSignature targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.ClaimId = sourceDto.ClaimId;
			targetDto.Signature = sourceDto.Signature;
			targetDto.LocalDate = sourceDto.LocalDate;
			targetDto.LocalTimeZone = sourceDto.LocalTimeZone;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<ClaimSignature> ToDto(IPagedResult<claim_signature> entityPage)
		{
			var dtos = new List<ClaimSignature>();
			foreach (var entity in entityPage.Results)
			{
				var dto = ClaimSignatureMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<ClaimSignature>
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
