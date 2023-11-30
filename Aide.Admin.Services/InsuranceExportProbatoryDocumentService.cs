using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Mapping;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aide.Admin.Services
{
    public interface IInsuranceExportProbatoryDocumentService
	{
		Task<IEnumerable<InsuranceExportProbatoryDocument>> GetAllInsuranceExportProbatoryDocuments();
		Task<IEnumerable<InsuranceExportProbatoryDocument>> GetExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId);
		Task<InsuranceExportProbatoryDocument> GetInsuranceExportProbatoryDocumentById(int insuranceExportProbatoryDocumentId);
		Task<IEnumerable<InsuranceExportProbatoryDocument>> GetInsuranceExportProbatoryDocumentsByInsuranceId(int insuranceCompanyId);
		Task<IEnumerable<InsuranceExportProbatoryDocument>> GetCustomizedExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId, EnumExportTypeId exportTypeId);
		Task UpsertInsuranceExportProbatoryDocuments(int insuranceCompanyId, EnumClaimTypeId claimTypeId, EnumExportTypeId exportTypeId, InsuranceExportProbatoryDocumentService.InsuranceExportProbatoryDocumentUpsertRequest[] dtos);
		Task<InsuranceExportProbatoryDocumentService.InsuranceExportSettings> GetInsuranceExportSettings(IInsuranceCompanyClaimTypeSettingsService insuranceCompanyClaimTypeSettingsService, InsuranceExportProbatoryDocumentService.InsuranceExportSettingsRequest request);
	}

	public class InsuranceExportProbatoryDocumentService : IInsuranceExportProbatoryDocumentService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly ICacheService _cacheService;
		private const string _cacheKeyNameForDtoInsuranceExportProbatoryDocuments = "Dto-List-InsuranceExportProbatoryDocument";
		private readonly IInsuranceProbatoryDocumentService _insuranceProbatoryDocumentService;
		private readonly IProbatoryDocumentService _probatoryDocumentService;
		private readonly IInsuranceCollageService _insuranceCollageService;

		#endregion

		#region Constructor

		public InsuranceExportProbatoryDocumentService(
			IServiceProvider serviceProvider,
			ICacheService cacheService,
			IInsuranceProbatoryDocumentService insuranceProbatoryDocumentService,
			IProbatoryDocumentService probatoryDocumentService,
			IInsuranceCollageService insuranceCollageService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
			_insuranceProbatoryDocumentService = insuranceProbatoryDocumentService ?? throw new ArgumentNullException(nameof(insuranceProbatoryDocumentService));
			_probatoryDocumentService = probatoryDocumentService ?? throw new ArgumentNullException(nameof(probatoryDocumentService));
			_insuranceCollageService = insuranceCollageService ?? throw new ArgumentNullException(nameof(insuranceCollageService));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<InsuranceExportProbatoryDocument>> GetAllInsuranceExportProbatoryDocuments()
		{
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceExportProbatoryDocuments))
			{
				var cachedDtos = _cacheService.Get<IEnumerable<InsuranceExportProbatoryDocument>>(_cacheKeyNameForDtoInsuranceExportProbatoryDocuments);
				return await LoadMetadata(cachedDtos).ConfigureAwait(false); // IMPORTANT: This is still needed because metadata is being cached on its own service
			}
			//End Cache

			IEnumerable<InsuranceExportProbatoryDocument> dtos = new List<InsuranceExportProbatoryDocument>();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<insurance_export_probatory_document>(transientContext);
				var query = from insurance_export_probatory_document in repository.TableNoTracking select insurance_export_probatory_document;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return dtos;
				dtos = InsuranceExportProbatoryDocumentMap.ToDto(entities);
			}

			//Begin Cache
			_cacheService.Set(_cacheKeyNameForDtoInsuranceExportProbatoryDocuments, dtos);
			//End Cache

			// IMPORTANT: This is still needed because metadata is being cached on its own service
			return await LoadMetadata(dtos).ConfigureAwait(false);
		}

		/// <summary>
		/// Build the list of all different type of documents that are associated to a service type.
		/// It includes the non-probatory documents like the collage(s) and also the deposit slip ONLY IF the setting is ENABLED at service type level.
		/// </summary>
		/// <param name="insuranceCompanyId"></param>
		/// <param name="claimTypeId"></param>
		/// <returns></returns>
		public async Task<IEnumerable<InsuranceExportProbatoryDocument>> GetExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId)
		{
			if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));

			// Step 1: Get the list of probatory documents that are currently associated to the service type
			// TODO: Load metadata/probatory documents (at least the name)
			var dtosx = await _insuranceProbatoryDocumentService.GetAllInsuranceProbatoryDocuments().ConfigureAwait(false);
			var currentProbatoryDocs = dtosx.Where(x => x.InsuranceCompanyId == insuranceCompanyId && x.ClaimTypeId == claimTypeId)
						.OrderBy(o1 => o1.GroupId)
						.ThenBy(o2 => o2.SortPriority)
						.Select(doc => new InsuranceExportProbatoryDocument
						{
							ExportTypeId = EnumExportTypeId.Unknown,
							InsuranceCompanyId = insuranceCompanyId,
							ClaimTypeId = claimTypeId,
							ExportDocumentTypeId = EnumExportDocumentTypeId.ProbatoryDocument,
							SortPriority = doc.SortPriority,
							ProbatoryDocumentId = doc.ProbatoryDocumentId,
							CollageId = null,
							Name = doc.ProbatoryDocument.Name
						});

			// Step 2: Get the list of collages that are associated to the service type
			// TODO: Load metadata/collages (at least the name)
			var collagesByServiceType = await _insuranceCollageService.GetInsuranceCollageListByInsuranceCompanyIdAndClaimTypeId(insuranceCompanyId, claimTypeId).ConfigureAwait(false);
			var collages = collagesByServiceType.Select(collage => new InsuranceExportProbatoryDocument
			{
				ExportTypeId = EnumExportTypeId.Unknown,
				InsuranceCompanyId = insuranceCompanyId,
				ClaimTypeId = claimTypeId,
				ExportDocumentTypeId = EnumExportDocumentTypeId.Collage,
				SortPriority = 999, // IMPORTANT: It's doesn't have a value so that it's being hard coded here
				ProbatoryDocumentId = null,
				CollageId = collage.Id,
				Name = collage.Name
			});

			// Put all documents together and send the result back to the caller
			var result = new List<InsuranceExportProbatoryDocument>();
			if (currentProbatoryDocs.Any())
			{
				result.AddRange(currentProbatoryDocs);
			}
			if (collages.Any())
			{
				result.AddRange(collages);
			}
			return result;
        }

		public async Task<InsuranceExportProbatoryDocument> GetInsuranceExportProbatoryDocumentById(int insuranceExportProbatoryDocumentId)
		{
			if (insuranceExportProbatoryDocumentId <= 0) throw new ArgumentException(nameof(insuranceExportProbatoryDocumentId));

			var dtos = await GetAllInsuranceExportProbatoryDocuments().ConfigureAwait(false);
			var dto = dtos.FirstOrDefault(b => b.Id == insuranceExportProbatoryDocumentId);
			if (dto == null) throw new NonExistingRecordCustomizedException();

			return dto;
		}

		public async Task<IEnumerable<InsuranceExportProbatoryDocument>> GetInsuranceExportProbatoryDocumentsByInsuranceId(int insuranceCompanyId)
		{
			if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));
			var dtosx = await GetAllInsuranceExportProbatoryDocuments().ConfigureAwait(false);
			var dtos = dtosx
						.Where(x => x.InsuranceCompanyId == insuranceCompanyId)
						.OrderBy(o1 => o1.ClaimTypeId)
						.ThenBy(o2 => o2.ExportTypeId)
						.ThenBy(o3 => o3.SortPriority)
						.ToList();
			return dtos;
		}

		/// <summary>
		/// Get the customized list of documents previosuly set at service type level for exporting
		/// </summary>
		/// <param name="insuranceCompanyId"></param>
		/// <param name="claimTypeId"></param>
		/// <param name="exportTypeId"></param>
		/// <returns></returns>
		public async Task<IEnumerable<InsuranceExportProbatoryDocument>> GetCustomizedExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId, EnumExportTypeId exportTypeId)
		{
			if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));
			var dtosx = await GetAllInsuranceExportProbatoryDocuments().ConfigureAwait(false);
			var dtos = dtosx
						.Where(x => x.InsuranceCompanyId == insuranceCompanyId && x.ClaimTypeId == claimTypeId && x.ExportTypeId == exportTypeId)
						.OrderBy(o2 => o2.SortPriority)
						.ToList();
			return dtos;
		}

		private async Task<InsuranceExportProbatoryDocument> LoadMetadata(InsuranceExportProbatoryDocument dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));
			var dtos = new List<InsuranceExportProbatoryDocument> { dto };
			await LoadMetadata(dtos).ConfigureAwait(false);
			return dto;
		}

		private async Task<IEnumerable<InsuranceExportProbatoryDocument>> LoadMetadata(IEnumerable<InsuranceExportProbatoryDocument> dtos)
		{
			if (dtos == null) throw new ArgumentNullException(nameof(dtos));
			if (dtos.Any())
			{
				// Probatory Document
				var probatoryDocumentIds = dtos.Where(w => w.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument && w.ProbatoryDocumentId.HasValue).Select(x => x.ProbatoryDocumentId.Value).ToArray();
				var probatoryDocuments = await _probatoryDocumentService.GetProbatoryDocumentListByIds(probatoryDocumentIds).ConfigureAwait(false);

				// Collage
				var collageIds = dtos.Where(w => w.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage && w.CollageId.HasValue).Select(x => x.CollageId.Value).ToArray();
				var collages = await _insuranceCollageService.GetInsuranceCollageListByIds(collageIds);

				// Attach metadata
				foreach (var dto in dtos)
				{
					if (dto.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument && dto.ProbatoryDocumentId.HasValue)
                    {
						dto.Name = probatoryDocuments.FirstOrDefault(x => x.Id == dto.ProbatoryDocumentId)?.Name;
					}
					if (dto.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage && dto.CollageId.HasValue)
					{
						dto.Name = collages.FirstOrDefault(x => x.Id == dto.CollageId)?.Name;
					}
				}
			}
			return dtos;
		}

		public async Task UpsertInsuranceExportProbatoryDocuments(int insuranceCompanyId, EnumClaimTypeId claimTypeId, EnumExportTypeId exportTypeId, InsuranceExportProbatoryDocumentUpsertRequest[] dtos)
		{
			if (dtos == null) throw new ArgumentNullException(nameof(dtos));
			// Initialize the sortpriority on each item
			var sortPriority = 1;
			foreach (var dto in dtos)
			{
				dto.SortPriority = sortPriority;
				sortPriority++;
			}

			// Get the current list of insurance export probatory documents given the insurance ID and the service type
			var currentDocs = await GetCustomizedExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(insuranceCompanyId, claimTypeId, exportTypeId).ConfigureAwait(false);

			// Prepare the list of insurance export probatory document IDs for deletion
			var docsDeleted = currentDocs.Where(currentDoc => !dtos.Any(dto => dto.ExportDocumentTypeId == currentDoc.ExportDocumentTypeId &&
									((dto.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument && dto.ProbatoryDocumentId == currentDoc.ProbatoryDocumentId)
									|| (dto.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage && dto.CollageId == currentDoc.CollageId))));

			// Prepare the list of insurance export probatory documents that will be added
			var docsAdded = dtos.Where(dto => !currentDocs.Any(currentDoc => dto.ExportDocumentTypeId == currentDoc.ExportDocumentTypeId &&
									((dto.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument && dto.ProbatoryDocumentId == currentDoc.ProbatoryDocumentId)
									|| (dto.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage && dto.CollageId == currentDoc.CollageId))));
			var newDocsAdded = new List<InsuranceExportProbatoryDocument>();
			foreach (var docAdded in docsAdded)
			{
				var newDocAdded = new InsuranceExportProbatoryDocument
				{
					ExportTypeId = exportTypeId,
					InsuranceCompanyId = insuranceCompanyId,
					ClaimTypeId = claimTypeId,
					ExportDocumentTypeId = docAdded.ExportDocumentTypeId,
					SortPriority = docAdded.SortPriority,
					ProbatoryDocumentId = docAdded.ProbatoryDocumentId,
					CollageId = docAdded.CollageId
				};
				newDocsAdded.Add(newDocAdded);
			}

			// Prepare the list of insurance export probatory documents for update
			var docsUpdated = new List<InsuranceExportProbatoryDocument>();
			foreach (var currentDoc in currentDocs)
			{
				var dto = dtos.FirstOrDefault(dto => dto.ExportDocumentTypeId == currentDoc.ExportDocumentTypeId 
					&& ((dto.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument && dto.ProbatoryDocumentId == currentDoc.ProbatoryDocumentId)
						|| (dto.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage && dto.ProbatoryDocumentId == currentDoc.ProbatoryDocumentId)) 
					&& (dto.SortPriority != currentDoc.SortPriority));
				if (dto != null)
				{
					var updatedDoc = InsuranceExportProbatoryDocumentMap.Clone(currentDoc);
					updatedDoc.SortPriority = dto.SortPriority;
					docsUpdated.Add(updatedDoc);
				}
			}

			// Persist the changes in db
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				// Delete the insurance export probatory documents that were recently removed
				await DeleteInsuranceExportProbatoryDocumentsByIds(insuranceCompanyId, claimTypeId, exportTypeId, docsDeleted, transientContext).ConfigureAwait(false);
				// Insert the insurance export probatory documents that were recently added
				await InsertInsuranceExportProbatoryDocuments(newDocsAdded, transientContext).ConfigureAwait(false);
                // Update the insurance export probatory documents that were changed recently
                await UpdateInsuranceExportProbatoryDocuments(insuranceCompanyId, claimTypeId, exportTypeId, docsUpdated, transientContext).ConfigureAwait(false);
            }

			// Perform cache update
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceExportProbatoryDocuments))
			{
				var cachedDtosx = await GetAllInsuranceExportProbatoryDocuments().ConfigureAwait(false);
				var cachedDtos = cachedDtosx.ToList();

				if (docsDeleted.Any())
				{
					cachedDtos.RemoveAll(cache => cache.InsuranceCompanyId == insuranceCompanyId && cache.ClaimTypeId == claimTypeId && cache.ExportTypeId == exportTypeId 
					&& docsDeleted.Any(deleted => cache.ExportDocumentTypeId == deleted.ExportDocumentTypeId 
					&& ((cache.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument && cache.ProbatoryDocumentId == deleted.ProbatoryDocumentId) || (cache.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage && cache.CollageId == deleted.CollageId))));
				}
				if (newDocsAdded.Any())
				{
					cachedDtos.AddRange(newDocsAdded);
				}
				if (docsUpdated.Any())
				{
					foreach (var docUpdated in docsUpdated)
					{
						//var currentDto = cachedDtos.Find(x => x.InsuranceCompanyId == insuranceCompanyId && x.ClaimTypeId == claimTypeId && x.ProbatoryDocumentId == docUpdated.ProbatoryDocumentId);
						var currentDto = cachedDtos.Find(cache => cache.InsuranceCompanyId == insuranceCompanyId && cache.ClaimTypeId == claimTypeId && cache.ExportTypeId == exportTypeId
						&& (cache.ExportDocumentTypeId == docUpdated.ExportDocumentTypeId
						&& ((cache.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument && cache.ProbatoryDocumentId == docUpdated.ProbatoryDocumentId) || (cache.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage && cache.CollageId == docUpdated.CollageId))));
						if (currentDto != null)
						{
							currentDto = InsuranceExportProbatoryDocumentMap.ToDto(docUpdated, currentDto);
							currentDto.DateModified = docUpdated.DateModified; // The mapping does not overwrite the DateModified
						}
					}
				}
				if (docsDeleted.Any() || newDocsAdded.Any() || docsUpdated.Any())
				{
					_cacheService.Set(_cacheKeyNameForDtoInsuranceExportProbatoryDocuments, cachedDtos);
				}
			}
			//End Cache
		}

		private async Task InsertInsuranceExportProbatoryDocuments(IEnumerable<InsuranceExportProbatoryDocument> dtos, DbContext transientContext)
		{
			if (dtos == null) throw new ArgumentNullException(nameof(dtos));
			if (transientContext == null) throw new ArgumentNullException(nameof(transientContext));
			if (dtos.Any())
			{
				var repository = new EfRepository<insurance_export_probatory_document>(transientContext);
				var entities = new List<insurance_export_probatory_document>();
				foreach (var dto in dtos)
				{
					dto.DateCreated = DateTime.UtcNow;
					dto.DateModified = DateTime.UtcNow;
					var entity = InsuranceExportProbatoryDocumentMap.ToEntity(dto);
					entities.Add(entity);
				}
				//Console.WriteLine(JsonConvert.SerializeObject(entities));
				await repository.InsertAsync(entities).ConfigureAwait(false);
			}
		}

		private async Task UpdateInsuranceExportProbatoryDocuments(int insuranceCompanyId, EnumClaimTypeId claimTypeId, EnumExportTypeId exportTypeId, IEnumerable<InsuranceExportProbatoryDocument> dtos, DbContext transientContext)
		{
			if (dtos == null) throw new ArgumentNullException(nameof(dtos));
			if (transientContext == null) throw new ArgumentNullException(nameof(transientContext));
			if (dtos.Any())
			{
				var repository = new EfRepository<insurance_export_probatory_document>(transientContext);
				var probatoryDocumentIds = dtos.Where(w => w.ProbatoryDocumentId.HasValue).Select(x => x.ProbatoryDocumentId.Value).ToArray(); // *** TODO: FIX IT
				//var whereExpression = $"insurance_company_id == {insuranceCompanyId} && claim_type_id == {(int)claimTypeId} && export_type_id == {(int)exportTypeId}"; // && ({SqlQueryHelper.BuildWhereOr("probatory_document_id", probatoryDocumentIds)})
				var baseQuery = from insurance_export_probatory_document in repository.Table
								where insurance_export_probatory_document.insurance_company_id == insuranceCompanyId
									&& insurance_export_probatory_document.claim_type_id == (int)claimTypeId
									&& insurance_export_probatory_document.export_type_id == (int)exportTypeId
								select insurance_export_probatory_document;

				// Probatory documents
				var probatoryDocsUpdated = dtos.Where(x => x.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument && x.ProbatoryDocumentId.HasValue);
                if (probatoryDocsUpdated.Any())
                {
					var ProbatoryDocumentIds = probatoryDocsUpdated.Select(x => x.ProbatoryDocumentId).ToArray();
					//var filters = $"{whereExpression} && export_document_type_id == {(int)EnumExportDocumentTypeId.ProbatoryDocument}";
					//filters += $" && ({SqlQueryHelper.BuildWhereOr("probatory_document_id", probatoryDocsUpdated.Select(doc => doc.ProbatoryDocumentId.Value))})";
					//var entities = await repository.GetAllAsync(filters).ConfigureAwait(false);
					var query = from q in baseQuery
								where q.export_document_type_id == (int)EnumExportDocumentTypeId.ProbatoryDocument
									&& ProbatoryDocumentIds.Contains(q.probatory_document_id)
								select q;
					var entities = await query.ToListAsync().ConfigureAwait(false);
					foreach (var dto in probatoryDocsUpdated)
                    {
                        var entity = entities.FirstOrDefault(x => x.probatory_document_id == dto.ProbatoryDocumentId);
                        if (entity != null)
                        {
                            dto.DateModified = DateTime.UtcNow;
                            entity = InsuranceExportProbatoryDocumentMap.ToEntity(dto, entity);
                        }
                    }
                    await repository.UpdateAsync(entities).ConfigureAwait(false);
                }

                // Collages
                var collagesUpdated = dtos.Where(x => x.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage && x.CollageId.HasValue);
				if (collagesUpdated.Any())
				{
					var CollageIds = collagesUpdated.Select(x => x.CollageId).ToArray();
					//var filters = $"{whereExpression} && export_document_type_id == {(int)EnumExportDocumentTypeId.Collage}";
					//filters += $" && ({SqlQueryHelper.BuildWhereOr("collage_id", collagesUpdated.Select(doc => doc.CollageId.Value))})";
					//var entities = await repository.GetAllAsync(filters).ConfigureAwait(false);
					var query = from q in baseQuery
								where q.export_document_type_id == (int)EnumExportDocumentTypeId.Collage
									&& CollageIds.Contains(q.collage_id)
								select q;
					var entities = await query.ToListAsync().ConfigureAwait(false);
					foreach (var dto in collagesUpdated)
					{
						var entity = entities.FirstOrDefault(x => x.collage_id == dto.CollageId);
						if (entity != null)
						{
							dto.DateModified = DateTime.UtcNow;
							entity = InsuranceExportProbatoryDocumentMap.ToEntity(dto, entity);
						}
					}
					await repository.UpdateAsync(entities).ConfigureAwait(false);
				}
			}
		}

		private async Task DeleteInsuranceExportProbatoryDocumentsByIds(int insuranceCompanyId, EnumClaimTypeId claimTypeId, EnumExportTypeId exportTypeId, IEnumerable<InsuranceExportProbatoryDocument> docsDeleted, DbContext transientContext)
		{
			if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));
			if (docsDeleted == null) throw new ArgumentNullException(nameof(docsDeleted));
			if (docsDeleted.Any())
			{
				var repository = new EfRepository<insurance_export_probatory_document>(transientContext);
				//var whereExpression = $"insurance_company_id == {insuranceCompanyId} && claim_type_id == {(int)claimTypeId} && export_type_id == {(int)exportTypeId}";
				var baseQuery = from insurance_export_probatory_document in repository.Table
								where insurance_export_probatory_document.insurance_company_id == insuranceCompanyId
									&& insurance_export_probatory_document.claim_type_id == (int)claimTypeId
									&& insurance_export_probatory_document.export_type_id == (int)exportTypeId
								select insurance_export_probatory_document;

				// Probatory documents
				var probatoryDocsDeleted = docsDeleted.Where(x => x.ExportDocumentTypeId == EnumExportDocumentTypeId.ProbatoryDocument && x.ProbatoryDocumentId.HasValue);
				if (probatoryDocsDeleted.Any())
                {
					var ProbatoryDocumentIds = probatoryDocsDeleted.Select(x => x.ProbatoryDocumentId).ToArray();
					//var filters = $"{whereExpression} && export_document_type_id == {(int)EnumExportDocumentTypeId.ProbatoryDocument}";
					//filters += $" && ({SqlQueryHelper.BuildWhereOr("probatory_document_id", probatoryDocsDeleted.Select(doc => doc.ProbatoryDocumentId.Value))})";
					//var entities = await repository.GetAllAsync(filters).ConfigureAwait(false);
					var query = from q in baseQuery
								where q.export_document_type_id == (int)EnumExportDocumentTypeId.ProbatoryDocument
									&& ProbatoryDocumentIds.Contains(q.probatory_document_id)
								select q;
					var entities = await query.ToListAsync().ConfigureAwait(false);
					if (entities.Any())
					{
						await repository.DeleteAsync(entities).ConfigureAwait(false);
					}
				}

				// Collages
				var collagesDeleted = docsDeleted.Where(x => x.ExportDocumentTypeId == EnumExportDocumentTypeId.Collage && x.CollageId.HasValue);
				if (collagesDeleted.Any())
				{
					var CollageIds = collagesDeleted.Select(x => x.CollageId).ToArray();
					//var filters = $"{whereExpression} && export_document_type_id == {(int)EnumExportDocumentTypeId.Collage}";
					//filters += $" && ({SqlQueryHelper.BuildWhereOr("collage_id", collagesDeleted.Select(doc => doc.CollageId.Value))})";
					//var entities = await repository.GetAllAsync(filters).ConfigureAwait(false);
					var query = from q in baseQuery
								where q.export_document_type_id == (int)EnumExportDocumentTypeId.Collage
									&& CollageIds.Contains(q.collage_id)
								select q;
					var entities = await query.ToListAsync().ConfigureAwait(false);
					if (entities.Any())
					{
						await repository.DeleteAsync(entities).ConfigureAwait(false);
					}
				}
			}
		}

		public async Task<InsuranceExportSettings> GetInsuranceExportSettings(IInsuranceCompanyClaimTypeSettingsService insuranceCompanyClaimTypeSettingsService, InsuranceExportSettingsRequest request)
        {
			if (insuranceCompanyClaimTypeSettingsService == null) throw new ArgumentNullException(nameof(insuranceCompanyClaimTypeSettingsService));
			
			bool isExportingCustomizedDocsOnly = false;
			var settingsDictionary = await insuranceCompanyClaimTypeSettingsService.GetByInsuranceCompanyId(request.InsuranceCompanyId).ConfigureAwait(false);
			if (settingsDictionary.Any() && settingsDictionary.ContainsKey((int)request.ClaimTypeId))
			{
				var settings = settingsDictionary[(int)request.ClaimTypeId];
				if (request.ExportTypeId == EnumExportTypeId.PDF && settings.IsExportingCustomizedDocsToPdf.HasValue)
                {
					isExportingCustomizedDocsOnly = settings.IsExportingCustomizedDocsToPdf.Value;
				}
				else if (request.ExportTypeId == EnumExportTypeId.ZIP && settings.IsExportingCustomizedDocsToZip.HasValue)
				{
					isExportingCustomizedDocsOnly =  settings.IsExportingCustomizedDocsToZip.Value;
				}
			}

			var insuranceExportSettings = new InsuranceExportSettings
			{
				IsExportingCustomizedDocsOnly = isExportingCustomizedDocsOnly,
				ExportDocuments = new List<ExportDocument>()
			};
			if (isExportingCustomizedDocsOnly)
            {
				var insuranceExportProbatoryDocuments = await GetCustomizedExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(request.InsuranceCompanyId, request.ClaimTypeId, request.ExportTypeId).ConfigureAwait(false);
				if (insuranceExportProbatoryDocuments.Any())
				{
					insuranceExportSettings.ExportDocuments = insuranceExportProbatoryDocuments.Select(x => new ExportDocument
					{
						ExportDocumentTypeId = x.ExportDocumentTypeId,
						SortPriority = x.SortPriority,
						ProbatoryDocumentId = x.ProbatoryDocumentId,
						CollageId = x.CollageId
					});
				}
			}
			else
            {
				var insuranceExportProbatoryDocuments = await GetExportProbatoryDocumentsByInsuranceIdAndClaimTypeId(request.InsuranceCompanyId, request.ClaimTypeId).ConfigureAwait(false);
				if (insuranceExportProbatoryDocuments.Any())
                {
					insuranceExportSettings.ExportDocuments = insuranceExportProbatoryDocuments.Select(x => new ExportDocument
					{
						ExportDocumentTypeId = x.ExportDocumentTypeId,
						SortPriority = x.SortPriority,
						ProbatoryDocumentId = x.ProbatoryDocumentId,
						CollageId = x.CollageId
					});
				}
			}
			return insuranceExportSettings;
		}

		#endregion

		#region Local classes

		public class InsuranceExportProbatoryDocumentUpsertRequest
		{
			public EnumExportDocumentTypeId ExportDocumentTypeId { get; set; }
			public int SortPriority { get; set; }
			public int? ProbatoryDocumentId { get; set; }
			public int? CollageId { get; set; }
		}

		public class InsuranceExportSettingsRequest
        {
			public int InsuranceCompanyId { get; set; } 
			public EnumClaimTypeId ClaimTypeId { get; set; }
			public EnumExportTypeId ExportTypeId { get; set; }

		}

		public class InsuranceExportSettings
        {
			public bool IsExportingCustomizedDocsOnly { get; set; }
			public IEnumerable<ExportDocument> ExportDocuments { get; set; }
		}

		public class ExportDocument
        {
			public EnumExportDocumentTypeId ExportDocumentTypeId { get; set; }
			public int SortPriority { get; set; }
			public int? ProbatoryDocumentId { get; set; }
			public int? CollageId { get; set; }
		}

		#endregion
	}
}
