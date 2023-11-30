using Aide.Admin.Domain.Mapping;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Aide.Admin.Services
{
	public interface IInsuranceCollageProbatoryDocumentService
	{
		Task<IEnumerable<InsuranceCollageProbatoryDocument>> GetAllInsuranceCollageProbatoryDocuments();
		Task<IEnumerable<InsuranceCollageProbatoryDocument>> GetInsuranceCollageProbatoryDocumentListByCollageId(int collageId);
		Task UpsertInsuranceCollageProbatoryDocuments(List<InsuranceCollageProbatoryDocument> collageDocuments, int collageId);
	}

	/// <summary>
	/// VERY IMPORTANT:
	/// If in the future the property of ProbatoryDocument is added to InsuranceCollageProbatoryDocument
	/// please visit the logic in InsuranceProbatoryDocumentService and check how the metadata is handled
	/// </summary>
	public class InsuranceCollageProbatoryDocumentService : IInsuranceCollageProbatoryDocumentService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly ICacheService _cacheService;
		private const string _cacheKeyNameForDtoInsuranceCollageProbatoryDocuments = "Dto-List-InsuranceCollageProbatoryDocument";

		#endregion

		#region Constructor

		public InsuranceCollageProbatoryDocumentService(IServiceProvider serviceProvider, ICacheService cacheService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_cacheService = cacheService ?? throw new ArgumentNullException();
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<InsuranceCollageProbatoryDocument>> GetAllInsuranceCollageProbatoryDocuments()
		{
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments))
			{
				return _cacheService.Get<IEnumerable<InsuranceCollageProbatoryDocument>>(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments);
			}
			//End Cache

			IEnumerable<InsuranceCollageProbatoryDocument> dtos = new List<InsuranceCollageProbatoryDocument>();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<insurance_collage_probatory_document>(transientContext);
				var query = from insurance_collage_probatory_document in repository.TableNoTracking select insurance_collage_probatory_document;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				dtos = InsuranceCollageProbatoryDocumentMap.ToDto(entities);
			}

			//Begin Cache
			_cacheService.Set(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments, dtos);
			//End Cache

			return dtos;
		}

		public async Task<IEnumerable<InsuranceCollageProbatoryDocument>> GetInsuranceCollageProbatoryDocumentListByCollageId(int collageId)
		{
			if (collageId <= 0) throw new ArgumentException(nameof(collageId));

			var dtos = await GetAllInsuranceCollageProbatoryDocuments().ConfigureAwait(false);
			var result = dtos.Where(x => x.InsuranceCollageId == collageId).OrderBy(o => o.SortPriority);
			return result.ToList();
		}

		public async Task UpsertInsuranceCollageProbatoryDocuments(List<InsuranceCollageProbatoryDocument> collageDocuments, int collageId)
		{
			if (collageDocuments == null) throw new ArgumentNullException(nameof(collageDocuments));
			if (collageDocuments.Any(doc => doc.InsuranceCollageId <= 0 || doc.InsuranceCollageId != collageId)) throw new ArgumentException(nameof(collageDocuments));
			if (collageId <= 0) throw new ArgumentException(nameof(collageId));

			// Initialize the sortpriority on each item
			var sortPriority = 1;
			foreach (var collageDocument in collageDocuments)
			{
				collageDocument.SortPriority = sortPriority;
				sortPriority++;
			}

			var currentDocuments = await GetInsuranceCollageProbatoryDocumentListByCollageId(collageId).ConfigureAwait(false);

			// Determine which docs were removed
			var deletedDocs = (from cd in currentDocuments
							   from ud in collageDocuments.Where(udx => udx.ProbatoryDocumentId == cd.ProbatoryDocumentId && udx.InsuranceCollageId == collageId).DefaultIfEmpty()
							   where cd.InsuranceCollageId == collageId && ud == null
							   select cd).ToList();

			// Determine which docs were modified/changed
			var updatedDocs = (from cd in currentDocuments
							   join ud in collageDocuments on cd.ProbatoryDocumentId equals ud.ProbatoryDocumentId
							   where cd.SortPriority != ud.SortPriority && cd.InsuranceCollageId == collageId && ud.InsuranceCollageId == collageId
							   select ud).ToList();

			// Determine which docs were added
			var addedDocs = (from ad in collageDocuments
							 from cd in currentDocuments.Where(cdx => cdx.ProbatoryDocumentId == ad.ProbatoryDocumentId && cdx.InsuranceCollageId == collageId).DefaultIfEmpty()
							 where ad.InsuranceCollageId == collageId && cd == null
							 select ad).ToList();

			// Apply changes to database
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<insurance_collage_probatory_document>(transientContext);

				if (deletedDocs.Any())
				{
					var deletedDocIds = deletedDocs.Select(x => x.Id).ToList();
					var query = from insurance_collage_probatory_document in repository.Table
								where insurance_collage_probatory_document.insurance_collage_id == collageId && deletedDocIds.Contains(insurance_collage_probatory_document.insurance_collage_probatory_document_id)
								select insurance_collage_probatory_document;
					var entities = await query.ToListAsync().ConfigureAwait(false);
					if (entities.Any())
					{
						await repository.DeleteAsync(entities).ConfigureAwait(false);
					}
				}

				if (updatedDocs.Any())
				{
					var updatedDocIds = updatedDocs.Select(x => x.ProbatoryDocumentId).ToList();
					var query = from insurance_collage_probatory_document in repository.Table
								where insurance_collage_probatory_document.insurance_collage_id == collageId && updatedDocIds.Contains(insurance_collage_probatory_document.probatory_document_id)
								select insurance_collage_probatory_document;
					var entities = await query.ToListAsync().ConfigureAwait(false);
					foreach (var updatedDoc in updatedDocs)
					{
						var entity = entities.FirstOrDefault(x => x.probatory_document_id == updatedDoc.ProbatoryDocumentId);
						if (entity != null)
						{
							updatedDoc.DateModified = DateTime.UtcNow;
							entity = InsuranceCollageProbatoryDocumentMap.ToEntity(updatedDoc, entity);
						}
					}
					await repository.UpdateAsync(entities).ConfigureAwait(false);
				}

				if (addedDocs.Any())
				{
					var entities = new List<insurance_collage_probatory_document>();
					foreach (var dto in addedDocs)
					{
						dto.DateCreated = DateTime.UtcNow;
						dto.DateModified = DateTime.UtcNow;
						var entity = InsuranceCollageProbatoryDocumentMap.ToEntity(dto);
						entities.Add(entity);
					}
					await repository.InsertAsync(entities).ConfigureAwait(false);
					// IMPORTANT: Below map the changes in entities back to the DTOs (i.e. insurance_collage_probatory_document_id)
					addedDocs = InsuranceCollageProbatoryDocumentMap.ToDto(entities).ToList();
				}
			}

			// Apply changes to cache
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments))
			{
				var cachedDtosx = await GetAllInsuranceCollageProbatoryDocuments().ConfigureAwait(false);
				var cachedDtos = cachedDtosx.ToList();

				if (deletedDocs.Any())
				{
					cachedDtos.RemoveAll(x => x.InsuranceCollageId == collageId && deletedDocs.Any(doc => doc.ProbatoryDocumentId == x.ProbatoryDocumentId));
				}

				if (updatedDocs.Any())
				{
					foreach (var updatedDoc in updatedDocs)
					{
						var currentDto = cachedDtos.Find(x => x.InsuranceCollageId == collageId && x.ProbatoryDocumentId == updatedDoc.ProbatoryDocumentId);
						if (currentDto != null)
						{
							currentDto = InsuranceCollageProbatoryDocumentMap.ToDto(updatedDoc, currentDto);
							currentDto.DateModified = updatedDoc.DateModified; // The mapping does not overwrite the DateModified
						}
					}
				}

				if (addedDocs.Any())
				{
					cachedDtos.AddRange(addedDocs);
				}

				if (deletedDocs.Any() || updatedDocs.Any() || addedDocs.Any())
				{
					_cacheService.Set(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments, cachedDtos);
				}
			}
			//End Cache
		}

		#endregion
	}
}
