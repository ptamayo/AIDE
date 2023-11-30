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
	public interface IClaimProbatoryDocumentService
	{
		Task<IEnumerable<ClaimProbatoryDocument>> GetAllClaimProbatoryDocumentsByClaimId(int claimId);
		Task<IEnumerable<ClaimProbatoryDocument>> GetClaimProbatoryDocumentListByClaimIds(ICollection<int> claimIds);
		Task<ClaimProbatoryDocument> GetClaimProbatoryDocumentById(int claimProbatoryDocumentId);
		Task InsertClaimProbatoryDocument(ClaimProbatoryDocument dto);
		Task<IEnumerable<ClaimProbatoryDocument>> InsertClaimProbatoryDocuments(IEnumerable<ClaimProbatoryDocument> dtos);
		Task<ClaimProbatoryDocumentMedia> AttachMedia(Media media, int claimProbatoryDocumentId);
		Task DeleteClaimProbatoryDocumentByClaimIdAndProbatoryDocumentId(int claimId, int probatoryDocumentId);
		Task DeleteClaimProbatoryDocumentByIds(ICollection<int> claimProbatoryDocumentIds);
	}

	public class ClaimProbatoryDocumentService : IClaimProbatoryDocumentService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly IProbatoryDocumentService _probatoryDocumentService;
		private readonly IMediaService _mediaService;
		private readonly IClaimProbatoryDocumentMediaService _claimProbatoryDocumentMediaService;

		#endregion

		#region Constructor

		public ClaimProbatoryDocumentService(
			IServiceProvider serviceProvider, 
			IProbatoryDocumentService probatoryDocumentService,
			IMediaService mediaService,
			IClaimProbatoryDocumentMediaService claimProbatoryDocumentMediaService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_probatoryDocumentService = probatoryDocumentService ?? throw new ArgumentNullException(nameof(probatoryDocumentService));
			_mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
			_claimProbatoryDocumentMediaService = claimProbatoryDocumentMediaService ?? throw new ArgumentNullException(nameof(claimProbatoryDocumentMediaService));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<ClaimProbatoryDocument>> GetAllClaimProbatoryDocumentsByClaimId(int claimId)
		{
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document>(transientContext);
				var query = from claim_probatory_document in repository.TableNoTracking
							where claim_probatory_document.claim_id == claimId
							select claim_probatory_document;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return null;

				var dtos = new List<ClaimProbatoryDocument>();
				foreach (var entity in entities)
				{
					var dto = ClaimProbatoryDocumentMap.ToDto(entity);
					dtos.Add(dto);
				}

				return dtos;
			}
		}

		public async Task<IEnumerable<ClaimProbatoryDocument>> GetClaimProbatoryDocumentListByClaimIds(ICollection<int> claimIds)
		{
			if (claimIds == null) throw new ArgumentNullException(nameof(claimIds));
			IEnumerable<ClaimProbatoryDocument> dtos;
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document>(transientContext);
				var query = from claim_probatory_document in repository.TableNoTracking
							where claimIds.Contains(claim_probatory_document.claim_id)
							orderby claim_probatory_document.group_id, claim_probatory_document.sort_priority
							select claim_probatory_document;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return new List<ClaimProbatoryDocument>();
				dtos = ClaimProbatoryDocumentMap.ToDto(entities);
			}

			// Probatory Documents
			var probatoryDocumentIds = dtos.Select(x => x.ProbatoryDocumentId).ToArray();
			var probatoryDocuments = await _probatoryDocumentService.GetProbatoryDocumentListByIds(probatoryDocumentIds).ConfigureAwait(false);

			// Claim Probatory Documents
			var claimProbatoryDocumentIds = dtos.Select(x => x.Id).ToArray();
			// Claim Probatory Document Medias
			var claimProbatoryDocumentMedias = await _claimProbatoryDocumentMediaService.GetClaimProbatoryDocumentMediaByClaimProbatoryDocumentIds(claimProbatoryDocumentIds).ConfigureAwait(false);
			// Medias
			var medias = new List<Media>();
			if (claimProbatoryDocumentMedias.Any())
			{
				var mediaIds = claimProbatoryDocumentMedias.Select(x => x.MediaId).ToArray();
				var dtosx = await _mediaService.GetMediaByIds(mediaIds).ConfigureAwait(false);
				medias = dtosx.ToList();
			}

			//var dtos = new List<ClaimProbatoryDocument>();
			foreach (var dto in dtos)
			{
				// Probatory Document
				//var dto = ClaimProbatoryDocumentMap.ToDto(entity);
				dto.ProbatoryDocument = probatoryDocuments.FirstOrDefault(x => x.Id == dto.ProbatoryDocumentId);
				// Media
				var claimProbatoryDocumentMedia = claimProbatoryDocumentMedias.FirstOrDefault(x => x.ClaimProbatoryDocumentId == dto.Id);
				if (claimProbatoryDocumentMedia != null)
				{
					dto.Media = medias.FirstOrDefault(x => x.Id == claimProbatoryDocumentMedia.MediaId);
				}
				//dtos.Add(dto);
			}

			return dtos;
		}

		public async Task<ClaimProbatoryDocument> GetClaimProbatoryDocumentById(int claimProbatoryDocumentId)
		{
			if (claimProbatoryDocumentId <= 0) throw new ArgumentOutOfRangeException(nameof(claimProbatoryDocumentId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document>(transientContext);
				var entity = await repository.GetByIdAsync(claimProbatoryDocumentId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				var dto = ClaimProbatoryDocumentMap.ToDto(entity);
				return dto;
			}
		}

		public async Task InsertClaimProbatoryDocument(ClaimProbatoryDocument dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			dto.DateCreated = DateTime.UtcNow;
			dto.DateModified = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = ClaimProbatoryDocumentMap.ToEntity(dto);
				var repository = new EfRepository<claim_probatory_document>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.claim_probatory_document_id;
			}
		}

		public async Task<IEnumerable<ClaimProbatoryDocument>> InsertClaimProbatoryDocuments(IEnumerable<ClaimProbatoryDocument> dtos)
		{
			if (dtos == null) throw new ArgumentNullException(nameof(dtos));
			foreach (var dto in dtos)
			{
				await this.InsertClaimProbatoryDocument(dto).ConfigureAwait(false);
			}
			return dtos;
		}

		public async Task<ClaimProbatoryDocumentMedia> AttachMedia(Media media, int claimProbatoryDocumentId)
		{
			if (media == null) throw new ArgumentNullException(nameof(media));
			if (claimProbatoryDocumentId <= 0) throw new ArgumentOutOfRangeException(nameof(claimProbatoryDocumentId));

			// Insert the new media
			await _mediaService.InsertMedia(media).ConfigureAwait(false);

			// Delete previous ClaimProbatoryDocumentMedia, if any, but don't delete the physical file yet.
			// The physical file should be deleted later in an automated cleansing process.
			try
			{
				await _claimProbatoryDocumentMediaService.DeleteClaimProbatoryDocumentMediaByClaimProbatoryDocumentId(claimProbatoryDocumentId).ConfigureAwait(false);
			}
			catch (NonExistingRecordCustomizedException) { }

			// Insert new ClaimProbatoryDocumentMedia
			var newClaimProbatoryDocumentMedia = new ClaimProbatoryDocumentMedia
			{
				ClaimProbatoryDocumentId = claimProbatoryDocumentId,
				MediaId = media.Id
			};
			await _claimProbatoryDocumentMediaService.InsertClaimProbatoryDocumentMedia(newClaimProbatoryDocumentMedia).ConfigureAwait(false);
			return newClaimProbatoryDocumentMedia;
		}

		public async Task DeleteClaimProbatoryDocumentByClaimIdAndProbatoryDocumentId(int claimId, int probatoryDocumentId)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));
			if (probatoryDocumentId <= 0) throw new ArgumentOutOfRangeException(nameof(probatoryDocumentId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document>(transientContext);
				var query = from claim_probatory_document in repository.Table
							where claim_probatory_document.claim_id == claimId && claim_probatory_document.probatory_document_id == probatoryDocumentId
							select claim_probatory_document;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (entities == null) throw new NonExistingRecordCustomizedException();

				foreach (var entity in entities.ToList())
				{
					await repository.DeleteAsync(entity).ConfigureAwait(false);
				}
			}
		}

		public async Task DeleteClaimProbatoryDocumentByIds(ICollection<int> claimProbatoryDocumentIds)
		{
			if (claimProbatoryDocumentIds == null) throw new ArgumentNullException(nameof(claimProbatoryDocumentIds));
			if (!claimProbatoryDocumentIds.Any()) return;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_probatory_document>(transientContext);
				var query = from claim_probatory_document in repository.Table
							where claimProbatoryDocumentIds.Contains(claim_probatory_document.claim_probatory_document_id)
							select claim_probatory_document;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (entities == null) throw new NonExistingRecordCustomizedException();

				foreach (var entity in entities.ToList())
				{
					await repository.DeleteAsync(entity).ConfigureAwait(false);
				}
			}
		}

		#endregion
	}
}
