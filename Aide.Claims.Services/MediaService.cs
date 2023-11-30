using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Claims.Domain.Mapping;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aide.Claims.Services
{
	public interface IMediaService
	{
		Task<IEnumerable<Media>> GetMediaByIds(int[] mediaIds);
		Task<Media> GetMediaById(int MediaId);
		Task InsertMedia(Media dto);
		Task UpdateMedia(Media dto);
		Task DeleteMedia(int MediaId);
	}

	public class MediaService : IMediaService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;

		#endregion

		#region Constructor

		public MediaService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<Media>> GetMediaByIds(int[] mediaIds)
		{
			if (mediaIds == null) throw new ArgumentException(nameof(mediaIds));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<media>(transientContext);
				var query = from media in repository.TableNoTracking
							where mediaIds.Contains(media.media_id)
							select media;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (entities == null) throw new NonExistingRecordCustomizedException();
				var dtos = MediaMap.ToDto(entities);
				return dtos;
			}
		}

		public async Task<Media> GetMediaById(int mediaId)
		{
			if (mediaId == 0) throw new ArgumentException();

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<media>(transientContext);
				var entity = await repository.GetByIdAsync(mediaId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				var dto = MediaMap.ToDto(entity);
				return dto;
			}
		}

		public async Task InsertMedia(Media dto)
		{
			if (dto == null) throw new ArgumentNullException();

			dto.DateCreated = DateTime.UtcNow;
			dto.DateModified = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = MediaMap.ToEntity(dto);
				var repository = new EfRepository<media>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.media_id;
			}
		}

		public async Task UpdateMedia(Media dto)
		{
			if (dto == null) throw new ArgumentNullException();
			if (dto.Id <= 0) throw new ArgumentException(nameof(dto.Id));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<media>(transientContext);
				var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				dto.DateModified = DateTime.UtcNow;
				entity = MediaMap.ToEntity(dto, entity);
				await repository.UpdateAsync(entity).ConfigureAwait(false);
			}
		}

		public async Task DeleteMedia(int mediaId)
		{
			if (mediaId <= 0) throw new ArgumentException(nameof(mediaId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<media>(transientContext);
				var entity = await repository.GetByIdAsync(mediaId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				await repository.DeleteAsync(entity).ConfigureAwait(false);
			}
		}

		#endregion
	}
}
