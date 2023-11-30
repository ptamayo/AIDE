using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Mapping;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aide.Admin.Services
{
	public interface IStoreService
	{
		Task<IEnumerable<Store>> GetAllStores();
		Task<IPagedResult<Store>> GetAllStores(IPagingSettings pagingSettings, StoreService.Filters filters);
		Task<Store> GetStoreById(int storeId);
		Task<IEnumerable<Store>> GetStoreListByStoreIds(int[] storeIds);
		Task InsertStore(Store dto);
		Task UpdateStore(Store dto);
	}

	public class StoreService : IStoreService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly ICacheService _cacheService;
		private const string _cacheKeyNameForDtoStores = "Dto-List-Store";

		#endregion

		#region Constructor

		public StoreService(IServiceProvider serviceProvider, ICacheService cacheService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_cacheService = cacheService ?? throw new ArgumentNullException();
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<Store>> GetAllStores()
		{
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoStores))
			{
				return _cacheService.Get<IEnumerable<Store>>(_cacheKeyNameForDtoStores);
			}
			//End Cache

			IEnumerable<Store> dtos = new List<Store>();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<store>(transientContext);
				var query = from store in repository.TableNoTracking select store;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return dtos;
				dtos = StoreMap.ToDto(entities);
			}

			// Order the list of stores before caching
			var orderedList = OrderStores(dtos);

			//Begin Cache
			_cacheService.Set(_cacheKeyNameForDtoStores, orderedList);
			//End Cache

			return orderedList;
		}

		public async Task<IPagedResult<Store>> GetAllStores(IPagingSettings pagingSettings, StoreService.Filters filters)
		{
			if (pagingSettings == null) throw new ArgumentNullException(nameof(pagingSettings));
			if (pagingSettings.PageNumber < 1 || pagingSettings.PageSize < 1) throw new ArgumentException(nameof(pagingSettings));
			if (filters == null) throw new ArgumentNullException(nameof(filters));

			var result = await GetAllStores().ConfigureAwait(false);
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
						  where Regex.IsMatch(x.SAPNumber, regexString, RegexOptions.IgnoreCase) ||
								Regex.IsMatch(x.Name, regexString, RegexOptions.IgnoreCase)
						  select x)
						  .ToList();
			}

			var p = EfRepository<Store>.Paginate(pagingSettings, result);
			if (!p.Results.Any()) return null;
			var pageResult = StoreMap.ToDto(p);
			return pageResult;
		}

		private IEnumerable<Store> OrderStores(IEnumerable<Store> stores)
        {
			return stores.OrderBy(o => o.Name);
        }

		public async Task<Store> GetStoreById(int storeId)
		{
			if (storeId <= 0) throw new ArgumentException(nameof(storeId));

			var dtos = await GetAllStores().ConfigureAwait(false);
			var dto = dtos.FirstOrDefault(b => b.Id == storeId);
			if (dto == null) throw new NonExistingRecordCustomizedException();

			return dto;
		}

		public async Task<IEnumerable<Store>> GetStoreListByStoreIds(int[] storeIds)
		{
			if (storeIds == null) throw new ArgumentNullException(nameof(storeIds));

			var dtos = await GetAllStores().ConfigureAwait(false);
			var result = dtos.Where(x => storeIds.Contains(x.Id));
			return result.ToList();
		}

		public async Task InsertStore(Store dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			dto.DateCreated = DateTime.UtcNow;
			dto.DateModified = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = StoreMap.ToEntity(dto);
				var repository = new EfRepository<store>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.store_id;
			}

			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoStores))
			{
				var dtos = await GetAllStores().ConfigureAwait(false);
				var list = dtos.ToList();
				list.Add(dto);
				var orderedList = OrderStores(list);
				_cacheService.Set(_cacheKeyNameForDtoStores, orderedList);
			}
			//End Cache
		}

		public async Task UpdateStore(Store dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));
			if (dto.Id <= 0) throw new ArgumentException(nameof(dto.Id));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<store>(transientContext);
				var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException(nameof(entity));

				dto.DateCreated = entity.date_created;
				dto.DateModified = DateTime.UtcNow;

				entity = StoreMap.ToEntity(dto, entity);
				await repository.UpdateAsync(entity).ConfigureAwait(false);
			}

			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoStores))
			{
				var dtos = await GetAllStores().ConfigureAwait(false);
				var list = dtos.ToList();
				var currentDto = list.Find(li => li.Id == dto.Id);
				currentDto = StoreMap.ToDto(dto, currentDto);
				var orderedList = OrderStores(list);
                _cacheService.Set(_cacheKeyNameForDtoStores, orderedList);
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
