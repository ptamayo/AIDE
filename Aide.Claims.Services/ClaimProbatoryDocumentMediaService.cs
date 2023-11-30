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
	public interface IClaimProbatoryDocumentMediaService
	{
		Task<ClaimProbatoryDocumentMedia> GetClaimProbatoryDocumentMediaById(int claimProbatoryDocumentMediaId);
		Task<IEnumerable<ClaimProbatoryDocumentMedia>> GetClaimProbatoryDocumentMediaByClaimProbatoryDocumentIds(int[] claimProbatoryDocumentIds);
		Task<ClaimProbatoryDocumentMedia> GetClaimProbatoryDocumentMediaByClaimProbatoryDocumentId(int claimProbatoryDocumentId);
		Task InsertClaimProbatoryDocumentMedia(ClaimProbatoryDocumentMedia dto);
		Task UpdateClaimProbatoryDocumentMedia(ClaimProbatoryDocumentMedia dto);
		Task DeleteClaimProbatoryDocumentMedia(int claimProbatoryDocumentMediaId);
		Task DeleteClaimProbatoryDocumentMediaByClaimProbatoryDocumentId(int claimProbatoryDocumentId);
	}

	public class ClaimProbatoryDocumentMediaService : IClaimProbatoryDocumentMediaService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;

		#endregion

		#region Constructor

		public ClaimProbatoryDocumentMediaService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		#endregion

		#region Methods

		public async Task<ClaimProbatoryDocumentMedia> GetClaimProbatoryDocumentMediaById(int claimProbatoryDocumentMediaId)
		{
			if (claimProbatoryDocumentMediaId <= 0) throw new ArgumentException(nameof(claimProbatoryDocumentMediaId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document_media>(transientContext);
				var entity = await repository.GetByIdAsync(claimProbatoryDocumentMediaId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				var dto = ClaimProbatoryDocumentMediaMap.ToDto(entity);
				return dto;
			}
		}

		public async Task<IEnumerable<ClaimProbatoryDocumentMedia>> GetClaimProbatoryDocumentMediaByClaimProbatoryDocumentIds(int[] claimProbatoryDocumentIds)
		{
			if (claimProbatoryDocumentIds == null) throw new ArgumentNullException(nameof(claimProbatoryDocumentIds));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document_media>(transientContext);
				var query = from claim_probatory_document_media in repository.TableNoTracking
							where claimProbatoryDocumentIds.Contains(claim_probatory_document_media.claim_probatory_document_id)
							select claim_probatory_document_media;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (entities == null) throw new NonExistingRecordCustomizedException();

				var dtos = ClaimProbatoryDocumentMediaMap.ToDto(entities);
				return dtos;
			}
		}

		public async Task<ClaimProbatoryDocumentMedia> GetClaimProbatoryDocumentMediaByClaimProbatoryDocumentId(int claimProbatoryDocumentId)
		{
			if (claimProbatoryDocumentId <= 0) throw new ArgumentException(nameof(claimProbatoryDocumentId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document_media>(transientContext);
				var entity = await repository.GetByIdAsync(claimProbatoryDocumentId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				var dto = ClaimProbatoryDocumentMediaMap.ToDto(entity);
				return dto;
			}
		}

		public async Task InsertClaimProbatoryDocumentMedia(ClaimProbatoryDocumentMedia dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			dto.DateCreated = DateTime.UtcNow;
			dto.DateModified = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = ClaimProbatoryDocumentMediaMap.ToEntity(dto);
				var repository = new EfRepository<claim_probatory_document_media>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.claim_probatory_document_media_id;
			}
		}

		public async Task UpdateClaimProbatoryDocumentMedia(ClaimProbatoryDocumentMedia dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));
			if (dto.Id <= 0) throw new ArgumentException(nameof(dto.Id));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document_media>(transientContext);
				var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				dto.DateModified = DateTime.UtcNow;
				entity = ClaimProbatoryDocumentMediaMap.ToEntity(dto, entity);
				await repository.UpdateAsync(entity).ConfigureAwait(false);
			}
		}

		public async Task DeleteClaimProbatoryDocumentMedia(int claimProbatoryDocumentMediaId)
		{
			if (claimProbatoryDocumentMediaId <= 0) throw new ArgumentException(nameof(claimProbatoryDocumentMediaId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document_media>(transientContext);
				var entity = await repository.GetByIdAsync(claimProbatoryDocumentMediaId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				await repository.DeleteAsync(entity).ConfigureAwait(false);
			}
		}

		public async Task DeleteClaimProbatoryDocumentMediaByClaimProbatoryDocumentId(int claimProbatoryDocumentId)
		{
			if (claimProbatoryDocumentId <= 0) throw new ArgumentException(nameof(claimProbatoryDocumentId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document_media>(transientContext);
				var query = from claim_probatory_document_media in repository.Table
							where claim_probatory_document_media.claim_probatory_document_id == claimProbatoryDocumentId
							select claim_probatory_document_media;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (entities == null) throw new NonExistingRecordCustomizedException();

				foreach(var entity in entities.ToList())
				{
					await repository.DeleteAsync(entity).ConfigureAwait(false);
				}
			}
		}

		#endregion
	}
}
