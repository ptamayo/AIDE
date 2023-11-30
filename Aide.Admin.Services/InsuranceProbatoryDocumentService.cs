using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Mapping;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aide.Admin.Services
{
    public interface IInsuranceProbatoryDocumentService
    {
        Task<IEnumerable<InsuranceProbatoryDocument>> GetAllInsuranceProbatoryDocuments();
        Task<InsuranceProbatoryDocument> GetInsuranceProbatoryDocumentById(int insuranceProbatoryDocumentId);
        Task<IEnumerable<InsuranceProbatoryDocument>> GetInsuranceProbatoryDocumentsByInsuranceId(int insuranceCompanyId);
        Task<IEnumerable<InsuranceProbatoryDocument>> GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId);
        Task UpsertInsuranceProbatoryDocuments(int insuranceCompanyId, EnumClaimTypeId claimTypeId, InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest[] dtos);
    }

    public class InsuranceProbatoryDocumentService : IInsuranceProbatoryDocumentService
    {
        #region Properties

        private readonly IServiceProvider _serviceProvider;
        private readonly ICacheService _cacheService;
        private const string _cacheKeyNameForDtoInsuranceProbatoryDocuments = "Dto-List-InsuranceProbatoryDocument";
        private readonly IProbatoryDocumentService _probatoryDocumentService;

        #endregion

        #region Constructor

        public InsuranceProbatoryDocumentService(
            IServiceProvider serviceProvider,
            ICacheService cacheService,
            IProbatoryDocumentService probatoryDocumentService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _probatoryDocumentService = probatoryDocumentService ?? throw new ArgumentNullException(nameof(probatoryDocumentService));
        }

        #endregion

        #region Methods

        public async Task<IEnumerable<InsuranceProbatoryDocument>> GetAllInsuranceProbatoryDocuments()
        {
            //Begin Cache
            if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceProbatoryDocuments))
            {
                var cachedDtos = _cacheService.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments);
                return await LoadMetadata(cachedDtos).ConfigureAwait(false); // IMPORTANT: This is still needed because metadata is being cached on its own service
            }
            //End Cache

            IEnumerable<InsuranceProbatoryDocument> dtos = new List<InsuranceProbatoryDocument>();
            using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
                var repository = new EfRepository<insurance_probatory_document>(transientContext);
                var query = from insurance_probatory_document in repository.TableNoTracking select insurance_probatory_document;
                var entities = await query.ToListAsync().ConfigureAwait(false);
                if (!entities.Any()) return dtos;
                dtos = InsuranceProbatoryDocumentMap.ToDto(entities);
            }

            //Begin Cache
            _cacheService.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, dtos);
            //End Cache

            // IMPORTANT: This is still needed because metadata is being cached on its own service
            return await LoadMetadata(dtos).ConfigureAwait(false);
        }

        public async Task<InsuranceProbatoryDocument> GetInsuranceProbatoryDocumentById(int insuranceProbatoryDocumentId)
        {
            if (insuranceProbatoryDocumentId <= 0) throw new ArgumentException(nameof(insuranceProbatoryDocumentId));

            var dtos = await GetAllInsuranceProbatoryDocuments().ConfigureAwait(false);
            var dto = dtos.FirstOrDefault(b => b.Id == insuranceProbatoryDocumentId);
            if (dto == null) throw new NonExistingRecordCustomizedException();

            return dto;
        }

        public async Task<IEnumerable<InsuranceProbatoryDocument>> GetInsuranceProbatoryDocumentsByInsuranceId(int insuranceCompanyId)
        {
            if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));
            var dtosx = await GetAllInsuranceProbatoryDocuments().ConfigureAwait(false);
            var dtos = dtosx
                        .Where(x => x.InsuranceCompanyId == insuranceCompanyId)
                        .OrderBy(o1 => o1.ClaimTypeId)
                        .ThenBy(o2 => o2.GroupId)
                        .ThenBy(o3 => o3.SortPriority)
                        .ToList();
            return dtos;
        }

        public async Task<IEnumerable<InsuranceProbatoryDocument>> GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId)
        {
            if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));
            var dtosx = await GetAllInsuranceProbatoryDocuments().ConfigureAwait(false);
            var dtos = dtosx
                        .Where(x => x.InsuranceCompanyId == insuranceCompanyId && x.ClaimTypeId == claimTypeId)
                        .OrderBy(o1 => o1.GroupId)
                        .ThenBy(o2 => o2.SortPriority)
                        .ToList();
            return dtos;
        }

        private async Task<IEnumerable<InsuranceProbatoryDocument>> LoadMetadata(IEnumerable<InsuranceProbatoryDocument> dtos)
        {
            if (dtos == null) throw new ArgumentNullException(nameof(dtos));
            if (dtos.Any())
            {
                // Probatory Document
                var probatoryDocumentIds = dtos.Select(x => x.ProbatoryDocumentId).ToArray();
                var probatoryDocuments = await _probatoryDocumentService.GetProbatoryDocumentListByIds(probatoryDocumentIds).ConfigureAwait(false);

                // Attach metadata
                foreach (var dto in dtos)
                {
                    dto.ProbatoryDocument = probatoryDocuments.First(x => x.Id == dto.ProbatoryDocumentId);
                }
            }
            return dtos;
        }

        private async Task<InsuranceProbatoryDocument> LoadMetadata(InsuranceProbatoryDocument dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            var dtos = new List<InsuranceProbatoryDocument> { dto };
            await LoadMetadata(dtos).ConfigureAwait(false);
            return dto;
        }

        public async Task UpsertInsuranceProbatoryDocuments(int insuranceCompanyId, EnumClaimTypeId claimTypeId, InsuranceProbatoryDocumentUpsertRequest[] dtos)
        {
            if (dtos == null) throw new ArgumentNullException(nameof(dtos));
            // Initialize the sortpriority on each item
            var sortPriority = 1;
            foreach (var dto in dtos)
            {
                dto.SortPriority = sortPriority;
                sortPriority++;
            }

            // Get the current list of insurance probatory documents given the insurance ID and the service type
            var currentDocs = await GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(insuranceCompanyId, claimTypeId).ConfigureAwait(false);

            // Prepare the list of insurance probatory document IDs for deletion
            var docsDeletedIds = currentDocs.Where(currentDoc => !dtos.Any(dto => dto.ProbatoryDocumentId == currentDoc.ProbatoryDocumentId))
                                    .Select(x => x.ProbatoryDocumentId)
                                    .ToArray();

            // Prepare the list of insurance probatory documents that will be added
            var docsAdded = dtos.Where(dto => !currentDocs.Any(currentDoc => currentDoc.ProbatoryDocumentId == dto.ProbatoryDocumentId));
            var newDocsAdded = new List<InsuranceProbatoryDocument>();
            foreach (var docAdded in docsAdded)
            {
                var newDocAdded = new InsuranceProbatoryDocument
                {
                    InsuranceCompanyId = insuranceCompanyId,
                    ClaimTypeId = claimTypeId,
                    ProbatoryDocumentId = docAdded.ProbatoryDocumentId,
                    SortPriority = docAdded.SortPriority,
                    GroupId = docAdded.GroupId
                };
                newDocsAdded.Add(newDocAdded);
            }

            // Prepare the list of insurance probatory documents for update
            var docsUpdated = new List<InsuranceProbatoryDocument>();
            foreach (var currentDoc in currentDocs)
            {
                var dto = dtos.FirstOrDefault(dto => dto.ProbatoryDocumentId == currentDoc.ProbatoryDocumentId && (dto.GroupId != currentDoc.GroupId || dto.SortPriority != currentDoc.SortPriority));
                if (dto != null)
                {
                    var updatedDoc = InsuranceProbatoryDocumentMap.Clone(currentDoc);
                    updatedDoc.GroupId = dto.GroupId;
                    updatedDoc.SortPriority = dto.SortPriority;
                    docsUpdated.Add(updatedDoc);
                }
            }

            // Persist the changes in db
            using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
                // Delete the insurance probatory documents that were recently removed
                await DeleteInsuranceProbatoryDocumentsByIds(insuranceCompanyId, claimTypeId, docsDeletedIds, transientContext).ConfigureAwait(false);
                // Insert the insurance probatory documents that were recently added
                await InsertInsuranceProbatoryDocuments(newDocsAdded, transientContext).ConfigureAwait(false);
                // Update the insurance probatory documents that were changed recently
                await UpdateInsuranceProbatoryDocuments(insuranceCompanyId, claimTypeId, docsUpdated, transientContext).ConfigureAwait(false);
            }

            // Perform cache update
            //Begin Cache
            if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceProbatoryDocuments))
            {
                var cachedDtosx = await GetAllInsuranceProbatoryDocuments().ConfigureAwait(false);
                var cachedDtos = cachedDtosx.ToList();

                if (docsDeletedIds.Any())
                {
                    cachedDtos.RemoveAll(x => x.InsuranceCompanyId == insuranceCompanyId && x.ClaimTypeId == claimTypeId && docsDeletedIds.Any(probatoryDocumentId => probatoryDocumentId == x.ProbatoryDocumentId));
                }
                if (newDocsAdded.Any())
                {
                    cachedDtos.AddRange(newDocsAdded);
                }
                if (docsUpdated.Any())
                {
                    foreach (var docUpdated in docsUpdated)
                    {
                        var currentDto = cachedDtos.Find(x => x.InsuranceCompanyId == insuranceCompanyId && x.ClaimTypeId == claimTypeId && x.ProbatoryDocumentId == docUpdated.ProbatoryDocumentId);
                        if (currentDto != null)
                        {
                            currentDto = InsuranceProbatoryDocumentMap.ToDto(docUpdated, currentDto);
                            currentDto.DateModified = docUpdated.DateModified; // The mapping does not overwrite the DateModified
                        }
                    }
                }
                if (docsDeletedIds.Any() || newDocsAdded.Any() || docsUpdated.Any())
                {
                    _cacheService.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, cachedDtos);
                }
            }
            //End Cache
        }

        private async Task InsertInsuranceProbatoryDocuments(IEnumerable<InsuranceProbatoryDocument> dtos, DbContext transientContext)
        {
            if (dtos == null) throw new ArgumentNullException(nameof(dtos));
            if (transientContext == null) throw new ArgumentNullException(nameof(transientContext));
            if (dtos.Any())
            {
                var repository = new EfRepository<insurance_probatory_document>(transientContext);
                var entities = new List<insurance_probatory_document>();
                foreach (var dto in dtos)
                {
                    dto.DateCreated = DateTime.UtcNow;
                    dto.DateModified = DateTime.UtcNow;
                    var entity = InsuranceProbatoryDocumentMap.ToEntity(dto);
                    entities.Add(entity);
                }
                //Console.WriteLine(JsonConvert.SerializeObject(entities));
                await repository.InsertAsync(entities).ConfigureAwait(false);
            }
        }

        private async Task UpdateInsuranceProbatoryDocuments(int insuranceCompanyId, EnumClaimTypeId claimTypeId, IEnumerable<InsuranceProbatoryDocument> dtos, DbContext transientContext)
        {
            if (dtos == null) throw new ArgumentNullException(nameof(dtos));
            if (transientContext == null) throw new ArgumentNullException(nameof(transientContext));
            if (dtos.Any())
            {
                var repository = new EfRepository<insurance_probatory_document>(transientContext);
                var probatoryDocumentIds = dtos.Select(x => x.ProbatoryDocumentId).ToArray();
                //var whereExpression = $"insurance_company_id == {insuranceCompanyId} && claim_type_id == {(int)claimTypeId} && ({SqlQueryHelper.BuildWhereOr("probatory_document_id", probatoryDocumentIds)})";
                //var entities = await repository.GetAllAsync(whereExpression).ConfigureAwait(false);
                var query = from insurance_probatory_document in repository.Table
                            where insurance_probatory_document.insurance_company_id == insuranceCompanyId
                                && insurance_probatory_document.claim_type_id == (int)claimTypeId
                                && probatoryDocumentIds.Contains(insurance_probatory_document.probatory_document_id)
                            select insurance_probatory_document;
                var entities = await query.ToListAsync().ConfigureAwait(false);
                foreach (var dto in dtos)
                {
                    var entity = entities.FirstOrDefault(x => x.probatory_document_id == dto.ProbatoryDocumentId);
                    if (entity != null)
                    {
                        dto.DateModified = DateTime.UtcNow;
                        entity = InsuranceProbatoryDocumentMap.ToEntity(dto, entity);
                    }
                }
                //Console.WriteLine(JsonConvert.SerializeObject(entities));
                await repository.UpdateAsync(entities).ConfigureAwait(false);
            }
        }

        private async Task DeleteInsuranceProbatoryDocumentsByIds(int insuranceCompanyId, EnumClaimTypeId claimTypeId, int[] insuranceProbatoryDocumentIds, DbContext transientContext)
        {
            if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));
            if (insuranceProbatoryDocumentIds == null) throw new ArgumentNullException(nameof(insuranceProbatoryDocumentIds));
            if (insuranceProbatoryDocumentIds.Any())
            {
                var repository = new EfRepository<insurance_probatory_document>(transientContext);
                //var whereExpression = $"insurance_company_id == {insuranceCompanyId} && claim_type_id == {(int)claimTypeId} && ({SqlQueryHelper.BuildWhereOr("probatory_document_id", insuranceProbatoryDocumentIds)})";
                //var entities = await repository.GetAllAsync(whereExpression).ConfigureAwait(false);
                var query = from insurance_probatory_document in repository.Table
                            where insurance_probatory_document.insurance_company_id == insuranceCompanyId
                                && insurance_probatory_document.claim_type_id == (int)claimTypeId
                                && insuranceProbatoryDocumentIds.Contains(insurance_probatory_document.probatory_document_id)
                            select insurance_probatory_document;
                var entities = await query.ToListAsync().ConfigureAwait(false);
                if (entities.Any())
                {
                    //Console.WriteLine(JsonConvert.SerializeObject(entities));
                    await repository.DeleteAsync(entities).ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region Local classes

        public class InsuranceProbatoryDocumentUpsertRequest
        {
            public int ProbatoryDocumentId { get; set; }
            public int GroupId { get; set; }
            public int SortPriority { get; set; }
        }

        #endregion
    }
}
