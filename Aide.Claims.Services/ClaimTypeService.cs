using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Mapping;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aide.Claims.Services
{
	public interface IClaimTypeService
	{
		Task<IEnumerable<ClaimType>> GetAllClaimTypes();
		Task<ClaimType> GetClaimTypeById(int ClaimTypeId);
		Task<IEnumerable<ClaimType>> GetClaimTypeListByIds(int[] claimTypeIds);
	}

	public class ClaimTypeService : IClaimTypeService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly ICacheService _cacheService;
		private const string _cacheKeyNameForDtoClaimTypes = "Dto-List-ClaimType";

		#endregion

		#region Constructor

		public ClaimTypeService(IServiceProvider serviceProvider, ICacheService cacheService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<ClaimType>> GetAllClaimTypes()
		{
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoClaimTypes))
			{
				return _cacheService.Get<IEnumerable<ClaimType>>(_cacheKeyNameForDtoClaimTypes);
			}
			//End Cache

			IEnumerable<ClaimType> dtos = new List<ClaimType>();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_type>(transientContext);
				var query = from claim_type in repository.TableNoTracking select claim_type;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return dtos;
				dtos = ClaimTypeMap.ToDto(entities);
			}

			//Begin Cache
			_cacheService.Set(_cacheKeyNameForDtoClaimTypes, dtos);
			//End Cache

			return dtos;
		}

		public async Task<ClaimType> GetClaimTypeById(int claimTypeId)
		{
			if (claimTypeId <= 0) throw new ArgumentException(nameof(claimTypeId));

			var dtos = await GetAllClaimTypes().ConfigureAwait(false);
			var dto = dtos.FirstOrDefault(b => b.Id == claimTypeId);
			if (dto == null) throw new NonExistingRecordCustomizedException();

			return dto;
		}

		public async Task<IEnumerable<ClaimType>> GetClaimTypeListByIds(int[] claimTypeIds)
		{
			if (claimTypeIds == null) throw new ArgumentNullException(nameof(claimTypeIds));

			var dtos = await GetAllClaimTypes().ConfigureAwait(false);
			var dtosx = dtos.Where(c => claimTypeIds.Contains(c.Id));
			if (dtosx == null) throw new NonExistingRecordCustomizedException();

			return dtosx;
		}

		#endregion
	}
}
