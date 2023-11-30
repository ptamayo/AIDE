using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Mapping;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aide.Admin.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Aide.Admin.Services
{
	public interface IInsuranceCompanyService
	{
		Task<IEnumerable<InsuranceCompany>> GetAllEnabledInsuranceCompanies(EnumViewDetail viewDetail);
		Task<IEnumerable<InsuranceCompany>> GetAllInsuranceCompanies(EnumViewDetail viewDetail);
		Task<IPagedResult<InsuranceCompany>> GetAllInsuranceCompanies(IPagingSettings pagingSettings, InsuranceCompanyService.Filters filters);
		Task<InsuranceCompany> GetInsuranceCompanyById(int insuranceCompanyId);
		Task<IEnumerable<InsuranceCompany>> GetInsuranceCompanyListByInsuranceCompanyIds(int[] insuranceCompanyIds);
		Task InsertInsuranceCompany(InsuranceCompany dto);
		Task UpdateInsuranceCompany(InsuranceCompany dto);
	}

	public class InsuranceCompanyService : IInsuranceCompanyService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly ICacheService _cacheService;
		private const string _cacheKeyNameForDtoInsuranceCompanies = "Dto-List-InsuranceCompany";
		private readonly IInsuranceCompanyClaimTypeSettingsService _insuranceCompanyClaimTypeSettingsService;

		#endregion

		#region Constructor

		public InsuranceCompanyService(IServiceProvider serviceProvider, ICacheService cacheService, IInsuranceCompanyClaimTypeSettingsService insuranceCompanyClaimTypeSettingsService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
			_insuranceCompanyClaimTypeSettingsService = insuranceCompanyClaimTypeSettingsService ?? throw new ArgumentNullException(nameof(insuranceCompanyClaimTypeSettingsService));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<InsuranceCompany>> GetAllEnabledInsuranceCompanies(EnumViewDetail viewDetail)
        {
			var dtos = await GetAllInsuranceCompanies(viewDetail).ConfigureAwait(false);
			return dtos.Where(x => x.IsEnabled).ToList();
        }

		public async Task<IEnumerable<InsuranceCompany>> GetAllInsuranceCompanies(EnumViewDetail viewDetail)
		{
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceCompanies))
			{
				var cachedDtos = _cacheService.Get<IEnumerable<InsuranceCompany>>(_cacheKeyNameForDtoInsuranceCompanies);
				if (viewDetail == EnumViewDetail.Extended)
				{
					return await LoadMetadata(cachedDtos).ConfigureAwait(false);
				}
				return cachedDtos;
			}
			//End Cache

			IEnumerable<InsuranceCompany> dtos = new List<InsuranceCompany>();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<insurance_company>(transientContext);
				var query = from insurance_company in repository.TableNoTracking select insurance_company;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return dtos;
				dtos = InsuranceCompanyMap.ToDto(entities);
			}

			// Order the list of insurance companies before caching
			var orderedList = OrderInsuranceCompanies(dtos);

			//Begin Cache
			_cacheService.Set(_cacheKeyNameForDtoInsuranceCompanies, orderedList);
			//End Cache

			if (viewDetail == EnumViewDetail.Extended)
            {
				return await LoadMetadata(orderedList).ConfigureAwait(false);
            }

			return orderedList;
		}

		public async Task<IPagedResult<InsuranceCompany>> GetAllInsuranceCompanies(IPagingSettings pagingSettings, InsuranceCompanyService.Filters filters)
		{
			if (pagingSettings == null) throw new ArgumentNullException(nameof(pagingSettings));
			if (pagingSettings.PageNumber < 1 || pagingSettings.PageSize < 1) throw new ArgumentException(nameof(pagingSettings));
			if (filters == null) throw new ArgumentNullException(nameof(filters));

			var result = await GetAllInsuranceCompanies(EnumViewDetail.Minimum).ConfigureAwait(false);
			if (result == null) return null;

			if (!string.IsNullOrWhiteSpace(filters.Keywords))
			{
				// Notice the keywords are NOT converted to lowercase and also RegexOptions.IgnoreCase is being applied.
				// This is because the search will be performed against a collection which is differently of a EF Model.
				// See ClaimService.GetAllClaims(... for an example of a different implementation.
				var keywords = filters.Keywords.EscapeRegexSpecialChars().Split(' ');
				var regex = new Regex(string.Join("|", keywords));
				var regexString = regex.ToString();

				result = (from x in result
						  where Regex.IsMatch(x.Name, regexString, RegexOptions.IgnoreCase)
						  select x)
						  .ToList();
			}

			var p = EfRepository<InsuranceCompany>.Paginate(pagingSettings, result);
			if (!p.Results.Any()) return null;
			var pageResult = InsuranceCompanyMap.ToDto(p);
			return pageResult;
		}

		private IEnumerable<InsuranceCompany> OrderInsuranceCompanies(IEnumerable<InsuranceCompany> insuranceCompanies)
		{
			return insuranceCompanies.OrderBy(o => o.Name);
		}

		public async Task<InsuranceCompany> GetInsuranceCompanyById(int insuranceCompanyId)
		{
			if (insuranceCompanyId <= 0) throw new ArgumentException(nameof(insuranceCompanyId));
			// IMPORTANT: Do not change the arg EnumViewDetail.Extended in the line below because we need the metadata that contains the claim type settings
			var dtos = await GetAllInsuranceCompanies(EnumViewDetail.Extended).ConfigureAwait(false);
			var dto = dtos.FirstOrDefault(b => b.Id == insuranceCompanyId);
			if (dto == null) throw new NonExistingRecordCustomizedException();
			return dto;
		}

		public async Task<IEnumerable<InsuranceCompany>> GetInsuranceCompanyListByInsuranceCompanyIds(int[] insuranceCompanyIds)
		{
			if (insuranceCompanyIds == null) throw new ArgumentNullException(nameof(insuranceCompanyIds));

			var dtos = await GetAllInsuranceCompanies(EnumViewDetail.Extended).ConfigureAwait(false);
			var result = dtos.Where(x => insuranceCompanyIds.Contains(x.Id));
			return result.ToList();
		}

		private async Task<IEnumerable<InsuranceCompany>> LoadMetadata(IEnumerable<InsuranceCompany> dtos)
		{
			var claimTypeSettings = await _insuranceCompanyClaimTypeSettingsService.GetAll().ConfigureAwait(false);
			var companies = new List<InsuranceCompany>();
			foreach(var dto in dtos)
            {
				var settings = claimTypeSettings.Where(x => x.InsuranceCompanyId == dto.Id).GroupBy(gb => gb.ClaimTypeId).ToDictionary(dk => (int)dk.Key, dv => dv.FirstOrDefault());
				dto.ClaimTypeSettings = settings;
				companies.Add(dto);
			}
			return companies;
		}

		private async Task<InsuranceCompany> LoadMetadata(InsuranceCompany dto)
		{
			var claimTypeSettings = await _insuranceCompanyClaimTypeSettingsService.GetByInsuranceCompanyId(dto.Id).ConfigureAwait(false);
			dto.ClaimTypeSettings = claimTypeSettings;
			return dto;
		}

		public async Task InsertInsuranceCompany(InsuranceCompany dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			dto.DateCreated = DateTime.UtcNow;
			dto.DateModified = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = InsuranceCompanyMap.ToEntity(dto);
				var repository = new EfRepository<insurance_company>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.insurance_company_id;
			}

			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceCompanies))
			{
				var dtos = await GetAllInsuranceCompanies(EnumViewDetail.Minimum).ConfigureAwait(false);
				var list = dtos.ToList();
				list.Add(dto);
				var orderedList = OrderInsuranceCompanies(list);
				_cacheService.Set(_cacheKeyNameForDtoInsuranceCompanies, orderedList);
			}
			//End Cache
		}

		public async Task UpdateInsuranceCompany(InsuranceCompany dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));
			if (dto.Id <= 0) throw new ArgumentException(nameof(dto.Id));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<insurance_company>(transientContext);
				var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException(nameof(entity));

				dto.DateCreated = entity.date_created;
				dto.DateModified = DateTime.UtcNow;

				entity = InsuranceCompanyMap.ToEntity(dto, entity);
				await repository.UpdateAsync(entity).ConfigureAwait(false);
			}

			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoInsuranceCompanies))
			{
				var dtos = await GetAllInsuranceCompanies(EnumViewDetail.Minimum).ConfigureAwait(false);
				var list = dtos.ToList();
				var currentDto = list.Find(li => li.Id == dto.Id);
				currentDto = InsuranceCompanyMap.ToDto(dto, currentDto);
				var orderedList = OrderInsuranceCompanies(list);
                _cacheService.Set(_cacheKeyNameForDtoInsuranceCompanies, orderedList);
            }
			//End Cache
		}

		#endregion

		#region Local classes

		public class Filters
		{
			public string Keywords { get; set; }
		}

		#endregion
	}
}
