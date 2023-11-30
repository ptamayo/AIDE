using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Models;
using System.Collections.Generic;
using Document = Aide.Claims.Domain.Objects.Document;

namespace Aide.Claims.Domain.Mapping
{
	public class DocumentMap : Mapper
	{
		public static Document ToDto(document entity)
		{
			if (entity == null) return null;

			var dto = new Document
			{
				Id = entity.document_id,
				MimeType = entity.mime_type,
				Filename = entity.filename,
				Url = !string.IsNullOrWhiteSpace(entity.url) ? entity.url : null,
				MetadataTitle = entity.metadata_title,
				MetadataAlt = entity.metadata_alt,
				MetadataCopyright = entity.metadata_copyright,
				ChecksumSha1 = entity.checksum_sha1,
				ChecksumMd5 = entity.checksum_md5,
				DateCreated = entity.date_created,
				DateModified = entity.date_modified
			};

			return dto;
		}

		public static IEnumerable<Document> ToDto(IEnumerable<document> entities)
		{
			if (entities == null) return null;

			var dtos = new List<Document>();
			foreach (var e in entities)
			{
				var dto = DocumentMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static document ToEntity(Document dto)
		{
			if (dto == null) return null;

			var entity = new document
			{
				mime_type = dto.MimeType,
				filename = dto.Filename,
				url = !string.IsNullOrWhiteSpace(dto.Url) ? dto.Url : null,
				metadata_title = dto.MetadataTitle,
				metadata_alt = dto.MetadataAlt,
				metadata_copyright = dto.MetadataCopyright,
				checksum_sha1 = dto.ChecksumSha1,
				checksum_md5 = dto.ChecksumMd5,
				date_created = dto.DateCreated,
				date_modified = dto.DateModified,
			};

			return entity;
		}

		public static document ToEntity(Document dto, document entity)
		{
			if (dto == null) return null;
			if (entity == null) return null;

			var e = entity;
			e.mime_type = dto.MimeType;
			e.filename = dto.Filename;
			e.url = !string.IsNullOrWhiteSpace(dto.Url) ? dto.Url : null;
			e.metadata_title = dto.MetadataTitle;
			e.metadata_alt = dto.MetadataAlt;
			e.metadata_copyright = dto.MetadataCopyright;
			e.checksum_sha1 = dto.ChecksumSha1;
			e.checksum_md5 = dto.ChecksumMd5;
			e.date_created = dto.DateCreated;
			e.date_modified = dto.DateModified;

			return entity;
		}

		public static Document ToDto(Document sourceDto, Document targetDto)
		{
			if (sourceDto == null) return null;

			targetDto.MimeType = sourceDto.MimeType;
			targetDto.Filename = sourceDto.Filename;
			targetDto.Url = !string.IsNullOrWhiteSpace(sourceDto.Url) ? sourceDto.Url : null;
			targetDto.MetadataTitle = sourceDto.MetadataTitle;
			targetDto.MetadataAlt = sourceDto.MetadataAlt;
			targetDto.MetadataCopyright = sourceDto.MetadataCopyright;
			targetDto.ChecksumSha1 = sourceDto.ChecksumSha1;
			targetDto.ChecksumMd5 = sourceDto.ChecksumMd5;
			//If the lines below are uncommented then they will break the Hangfire Worker
			//targetDto.DateCreated = sourceDto.DateCreated;
			//targetDto.DateModified = sourceDto.DateModified;

			return targetDto;
		}

		public static IPagedResult<Document> ToDto(IPagedResult<document> entityPage)
		{
			var dtos = new List<Document>();
			foreach (var entity in entityPage.Results)
			{
				var dto = DocumentMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<Document>
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
