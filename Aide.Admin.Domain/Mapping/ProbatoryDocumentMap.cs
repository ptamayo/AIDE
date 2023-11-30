using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using System.Collections.Generic;
using Aide.Admin.Domain.Enumerations;

namespace Aide.Admin.Domain.Mapping
{
	public class ProbatoryDocumentMap : Mapper
	{
		public static ProbatoryDocument ToDto(probatory_document entity)
		{
			if (entity == null) return null;

			var dto = new ProbatoryDocument
			{
				Id = entity.probatory_document_id,
				Name = entity.probatory_document_name,
				Orientation = (EnumDocumentOrientationId)entity.probatory_document_orientation,
				AcceptedFileExtensions = entity.accepted_file_extensions,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<ProbatoryDocument> ToDto(IEnumerable<probatory_document> entities)
		{
			if (entities == null) return null;

			var dtos = new List<ProbatoryDocument>();
			foreach (var e in entities)
			{
				var dto = ProbatoryDocumentMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static probatory_document ToEntity(ProbatoryDocument dto)
		{
			if (dto == null) return null;

			var entity = new probatory_document
			{
				probatory_document_name = dto.Name,
				probatory_document_orientation = (int)dto.Orientation,
				accepted_file_extensions = dto.AcceptedFileExtensions,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static probatory_document ToEntity(ProbatoryDocument dto, probatory_document entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.probatory_document_name = dto.Name;
			e.probatory_document_orientation = (int)dto.Orientation;
			e.accepted_file_extensions = dto.AcceptedFileExtensions;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static ProbatoryDocument ToDto(ProbatoryDocument sourceDto, ProbatoryDocument targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.Name = sourceDto.Name;
			targetDto.Orientation = sourceDto.Orientation;
			targetDto.AcceptedFileExtensions = sourceDto.AcceptedFileExtensions;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<ProbatoryDocument> ToDto(IPagedResult<probatory_document> entityPage)
		{
			var dtos = new List<ProbatoryDocument>();
			foreach (var entity in entityPage.Results)
			{
				var dto = ProbatoryDocumentMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<ProbatoryDocument>
			{
				Results = dtos,
				CurrentPage = entityPage.CurrentPage,
				PageSize = entityPage.PageSize,
				PageCount = entityPage.PageCount,
				RowCount = entityPage.RowCount
			};
			return pageResult;
		}

		public static IPagedResult<ProbatoryDocument> ToDto(IPagedResult<ProbatoryDocument> dtoPage)
		{
			var dtos = new List<ProbatoryDocument>();
			foreach (var dto in dtoPage.Results)
			{
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<ProbatoryDocument>
			{
				Results = dtos,
				CurrentPage = dtoPage.CurrentPage,
				PageSize = dtoPage.PageSize,
				PageCount = dtoPage.PageCount,
				RowCount = dtoPage.RowCount
			};
			return pageResult;
		}
	}
}
