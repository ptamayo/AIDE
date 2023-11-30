using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using System.Collections.Generic;

namespace Aide.Claims.Domain.Mapping
{
	public class DocumentTypeMap : Mapper
	{
		public static DocumentType ToDto(document_type entity)
		{
			if (entity == null) return null;

			var dto = new DocumentType
			{
				Id = entity.document_type_id,
				Name = entity.document_type_name,
				AcceptedFileExtensions = entity.accepted_file_extensions,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<DocumentType> ToDto(IEnumerable<document_type> entities)
		{
			if (entities == null) return null;

			var dtos = new List<DocumentType>();
			foreach (var e in entities)
			{
				var dto = DocumentTypeMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static document_type ToEntity(DocumentType dto)
		{
			if (dto == null) return null;

			var entity = new document_type
			{
				document_type_name = dto.Name,
				accepted_file_extensions = dto.AcceptedFileExtensions,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static document_type ToEntity(DocumentType dto, document_type entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.document_type_name = dto.Name;
			e.accepted_file_extensions = dto.AcceptedFileExtensions;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static DocumentType ToDto(DocumentType sourceDto, DocumentType targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.Name = sourceDto.Name;
			targetDto.AcceptedFileExtensions = sourceDto.AcceptedFileExtensions;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<DocumentType> ToDto(IPagedResult<document_type> entityPage)
		{
			var dtos = new List<DocumentType>();
			foreach (var entity in entityPage.Results)
			{
				var dto = DocumentTypeMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<DocumentType>
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
