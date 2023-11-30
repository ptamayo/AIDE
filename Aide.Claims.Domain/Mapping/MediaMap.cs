using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using System.Collections.Generic;

namespace Aide.Claims.Domain.Mapping
{
	public class MediaMap : Mapper
	{
		public static Media ToDto(media entity)
		{
			if (entity == null) return null;

			var dto = new Media
			{
				Id = entity.media_id,
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

		public static IEnumerable<Media> ToDto(IEnumerable<media> entities)
		{
			if (entities == null) return null;

			var dtos = new List<Media>();
			foreach (var e in entities)
			{
				var dto = MediaMap.ToDto(e);
				dtos.Add(dto);
			}

			return dtos;
		}

		public static media ToEntity(Media dto)
		{
			if (dto == null) return null;

			var entity = new media
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

		public static media ToEntity(Media dto, media entity)
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

		public static Media ToDto(Media sourceDto, Media targetDto)
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

		public static IPagedResult<Media> ToDto(IPagedResult<media> entityPage)
		{
			var dtos = new List<Media>();
			foreach (var entity in entityPage.Results)
			{
				var dto = MediaMap.ToDto(entity);
				dtos.Add(dto);
			}
			var pageResult = new PagedResult<Media>
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
