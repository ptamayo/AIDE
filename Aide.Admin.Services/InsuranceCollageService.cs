using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Mapping;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Aide.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Aide.Admin.Services
{
	public interface IInsuranceCollageService
	{
		Task<IEnumerable<InsuranceCollage>> GetAllInsuranceCollages();
		Task<IPagedResult<InsuranceCollage>> GetAllInsuranceCollages(IPagingSettings pagingSettings, InsuranceCollageService.Filters filters);
		Task<InsuranceCollage> GetInsuranceCollageById(int collageId);
		Task<IEnumerable<InsuranceCollage>> GetInsuranceCollageListByIds(int[] collageIds);
		Task<IEnumerable<InsuranceCollage>> GetInsuranceCollageListByInsuranceCompanyId(int insuranceCompanyId);
		Task<IEnumerable<InsuranceCollage>> GetInsuranceCollageListByInsuranceCompanyIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId);
		Task InsertInsuranceCollage(InsuranceCollage dto);
		Task UpdateInsuranceCollage(InsuranceCollage dto);
		Task DeleteInsuranceCollage(int collageId);
	}

	public class InsuranceCollageService : IInsuranceCollageService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly ICacheService _cacheService;
		private const string _cacheKeyNameForDtoInsuranceCollages = "Dto-List-InsuranceCollage";
		private readonly IInsuranceCollageProbatoryDocumentService _insuranceCollageProbatoryDocumentService;
		private readonly IClaimTypeService _claimTypeService;

		#endregion

		#region Constructor

		public InsuranceCollageService(IServiceProvider serviceProvider, ICacheService cacheService, IInsuranceCollageProbatoryDocumentService insuranceCollageProbatoryDocumentService, IClaimTypeService claimTypeService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
			_insuranceCollageProbatoryDocumentService = insuranceCollageProbatoryDocumentService ?? throw new ArgumentNullException(nameof(insuranceCollageProbatoryDocumentService));
			_claimTypeService = claimTypeService ?? throw new ArgumentNullException(nameof(claimTypeService));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<InsuranceCollage>> GetAllInsuranceCollages()
		{
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceCollages))
			{
				var cachedDtos = _cacheService.Get<IEnumerable<InsuranceCollage>>(_cacheKeyNameForDtoInsuranceCollages);
				return await LoadMetadata(cachedDtos).ConfigureAwait(false); // IMPORTANT: This is still needed because metadata is being cached on its own service
			}
			//End Cache

			IEnumerable<InsuranceCollage> dtos = new List<InsuranceCollage>();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<insurance_collage>(transientContext);
				var query = from insurance_collage in repository.TableNoTracking select insurance_collage;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				dtos = InsuranceCollageMap.ToDto(entities);
			}

			//Begin Cache
			_cacheService.Set(_cacheKeyNameForDtoInsuranceCollages, dtos);
			//End Cache

			// IMPORTANT: This is still needed because metadata is being cached on its own service
			return await LoadMetadata(dtos).ConfigureAwait(false);
		}

		public async Task<IPagedResult<InsuranceCollage>> GetAllInsuranceCollages(IPagingSettings pagingSettings, InsuranceCollageService.Filters filters)
		{
			if (pagingSettings == null) throw new ArgumentNullException(nameof(pagingSettings));
			if (pagingSettings.PageNumber < 1 || pagingSettings.PageSize < 1) throw new ArgumentException(nameof(pagingSettings));
			if (filters == null) throw new ArgumentNullException(nameof(filters));

			var collages = await GetAllInsuranceCollages().ConfigureAwait(false);
			var query = InsuranceCollageMap.Clone(collages).AsQueryable(); // This is very important to prevent changes in the cache

			// Filter by keyword
			if (!string.IsNullOrWhiteSpace(filters.Keywords))
			{
				// Notice the keywords are converted to lowercase. Also there's no need to apply RegexOptions.IgnoreCase.
				// This is because the search will be performed against an EF Model.
				// See InsuranceCompanyService.GetAllClaims(... for an example of a different implementation.
				var keywords = filters.Keywords.EscapeRegexSpecialChars().ToLower().Split(' ');
				var regex = new Regex(string.Join("|", keywords));
				var regexString = regex.ToString();

				// Notice that RegexOptions.IgnoreCase has been applied below.
				// This is becuse the regexp is being applied to a Generic Collection.
				// When regex is applied to a EF object then this is not necessary.
				query = query.Where(x => 1 == 1 &&
								   (Regex.IsMatch(x.Name, regexString, RegexOptions.IgnoreCase)));
			}

			// Filter by company ID
			if (filters.CompanyId.HasValue)
			{
				if (filters.CompanyId.Value != 0)
				{
					query = query.Where(x => x.InsuranceCompanyId == filters.CompanyId.Value);
				}
			}

			// Filter by ID ascending
			query = query.OrderBy(o => o.Id);

			var page = EfRepository<InsuranceCollage>.Paginate(pagingSettings, query);
			
			// APPLY VIEW(S) HERE IF ANY

			return page;
		}

		public async Task<IEnumerable<InsuranceCollage>> GetInsuranceCollageListByInsuranceCompanyId(int insuranceCompanyId)
		{
			if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));

			var dtos = await GetAllInsuranceCollages().ConfigureAwait(false);
			var result = dtos.Where(x => x.InsuranceCompanyId == insuranceCompanyId);
			return result.ToList();
		}

		public async Task<IEnumerable<InsuranceCollage>> GetInsuranceCollageListByInsuranceCompanyIdAndClaimTypeId(int insuranceCompanyId, EnumClaimTypeId claimTypeId)
		{
			if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));
			if (claimTypeId <= 0) throw new ArgumentException(nameof(claimTypeId));

			var collages = await GetInsuranceCollageListByInsuranceCompanyId(insuranceCompanyId).ConfigureAwait(false);
			if (collages.Any())
			{
				var collageList = collages.Where(x => x.ClaimTypeId == claimTypeId).ToList();
				if (collageList != null)
				{
					return collageList;
				}
				else
				{
					throw new NonExistingRecordCustomizedException();
				}
			}
			else
			{
				throw new NonExistingRecordCustomizedException();
			}
		}

		public async Task<InsuranceCollage> GetInsuranceCollageById(int collageId)
		{
			if (collageId <= 0) throw new ArgumentException(nameof(collageId));

			var dtos = await GetAllInsuranceCollages().ConfigureAwait(false);
			var dto = dtos.FirstOrDefault(b => b.Id == collageId);
			if (dto == null) throw new NonExistingRecordCustomizedException();

			return dto;
		}

		public async Task<IEnumerable<InsuranceCollage>> GetInsuranceCollageListByIds(int[] collageIds)
		{
			if (collageIds == null) throw new ArgumentNullException(nameof(collageIds));
			var dtos = await GetAllInsuranceCollages().ConfigureAwait(false);
			var result = dtos.Where(x => collageIds.Contains(x.Id)).ToList();
			return result;
		}

		public async Task<IEnumerable<InsuranceCollage>> LoadMetadata(IEnumerable<InsuranceCollage> collages)
		{
			if (collages == null) throw new ArgumentNullException(nameof(collages));
			if (collages.Any())
			{
				var claimTypes = await _claimTypeService.GetAllClaimTypes().ConfigureAwait(false);
				foreach (var collage in collages)
				{
					var collageDocs = await _insuranceCollageProbatoryDocumentService.GetInsuranceCollageProbatoryDocumentListByCollageId(collage.Id).ConfigureAwait(false);
					collage.ProbatoryDocuments = collageDocs.ToList();
					collage.ClaimType = claimTypes.FirstOrDefault(x => x.Id == (int)collage.ClaimTypeId);
				}
			}
			return collages;
		}

		public async Task InsertInsuranceCollage(InsuranceCollage dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			dto.DateCreated = DateTime.UtcNow;
			dto.DateModified = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = InsuranceCollageMap.ToEntity(dto);
				var repository = new EfRepository<insurance_collage>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.insurance_collage_id;
			}

			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceCollages))
			{
				var dtosx = await GetAllInsuranceCollages().ConfigureAwait(false);
				var dtos = dtosx.ToList();
				dtos.Add(dto);
				_cacheService.Set(_cacheKeyNameForDtoInsuranceCollages, dtos);
			}
			//End Cache

			// Upsert the probatory documents in this collage
			foreach (var doc in dto.ProbatoryDocuments)
			{
				doc.InsuranceCollageId = dto.Id;
			}
			await _insuranceCollageProbatoryDocumentService.UpsertInsuranceCollageProbatoryDocuments(dto.ProbatoryDocuments.ToList(), dto.Id).ConfigureAwait(false);
		}

		public async Task UpdateInsuranceCollage(InsuranceCollage dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));
			if (dto.Id <= 0) throw new ArgumentException(nameof(dto.Id));
			if (dto.ProbatoryDocuments == null) throw new ArgumentNullException(nameof(dto.ProbatoryDocuments));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<insurance_collage>(transientContext);
				var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException(nameof(entity));

				dto.DateCreated = entity.date_created;
				dto.DateModified = DateTime.UtcNow;

				entity = InsuranceCollageMap.ToEntity(dto, entity);
				await repository.UpdateAsync(entity).ConfigureAwait(false);
			}

			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceCollages))
			{
				var dtosx = await GetAllInsuranceCollages().ConfigureAwait(false);
				var dtos = dtosx.ToList();
				var currentDto = dtos.Find(b => b.Id == dto.Id);
				currentDto = InsuranceCollageMap.ToDto(dto, currentDto);
				_cacheService.Set(_cacheKeyNameForDtoInsuranceCollages, dtos);
			}
			//End Cache

			// Upsert the probatory documents in this collage
			await _insuranceCollageProbatoryDocumentService.UpsertInsuranceCollageProbatoryDocuments(dto.ProbatoryDocuments.ToList(), dto.Id).ConfigureAwait(false);
		}

		public async Task DeleteInsuranceCollage(int collageId)
		{
			if (collageId <= 0) throw new ArgumentException(nameof(collageId));

			// First remove all probatory docs associated to the collage
			var emptyList = new List<InsuranceCollageProbatoryDocument>();
			await _insuranceCollageProbatoryDocumentService.UpsertInsuranceCollageProbatoryDocuments(emptyList, collageId).ConfigureAwait(false);

			// Finally delete the collage from database
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<insurance_collage>(transientContext);
				var whereExpression = $"insurance_collage_id == {collageId}";
				var entity = await repository.GetByIdAsync(collageId).ConfigureAwait(false);
				if (entity != null)
				{
					await repository.DeleteAsync(entity).ConfigureAwait(false);
				}
			}

			// Also delete the collage from cache
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceCollages))
			{
				var cache = await GetAllInsuranceCollages().ConfigureAwait(false);
				var cachedDtos = cache.ToList();
				cachedDtos.RemoveAll(x => x.Id == collageId);
				_cacheService.Set(_cacheKeyNameForDtoInsuranceCollages, cachedDtos);
			}
			//End Cache
		}

		#endregion

		#region Local classes

		public class Filters
		{
			public string Keywords { get; set; }
			public int? CompanyId { get; set; }
		}

		#endregion
	}
}
