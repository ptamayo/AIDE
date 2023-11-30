using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Interfaces;
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
	public interface IClaimDocumentTypeService
	{
		Task<IEnumerable<ClaimDocumentType>> GetAllClaimDocumentTypes();
		Task<ClaimDocumentType> GetClaimDocumentTypeById(int ClaimDocumentTypeId);
	}

	public class ClaimDocumentTypeService : IClaimDocumentTypeService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly ICacheService _cacheService;
		private const string _cacheKeyNameForDtoClaimDocumentTypes = "Dto-List-ClaimDocumentType";
		private readonly IDocumentTypeService _documentTypeService;

		#endregion

		#region Constructor

		public ClaimDocumentTypeService(IServiceProvider serviceProvider, IDocumentTypeService documentTypeService, ICacheService cacheService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_documentTypeService = documentTypeService ?? throw new ArgumentNullException(nameof(documentTypeService));
			_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<ClaimDocumentType>> GetAllClaimDocumentTypes()
		{
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoClaimDocumentTypes))
			{
				var cachedDtos = _cacheService.Get<IEnumerable<ClaimDocumentType>>(_cacheKeyNameForDtoClaimDocumentTypes);
				return await LoadMetadata(cachedDtos).ConfigureAwait(false);
			}
			//End Cache

			IEnumerable<ClaimDocumentType> dtos = new List<ClaimDocumentType>();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_document_type>(transientContext);
				var query = from claim_document_type in repository.TableNoTracking select claim_document_type;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return dtos;
				dtos = ClaimDocumentTypeMap.ToDto(entities);
			}

			//Begin Cache
			_cacheService.Set(_cacheKeyNameForDtoClaimDocumentTypes, dtos);
			//End Cache

			return await LoadMetadata(dtos).ConfigureAwait(false);
		}

		private async Task<IEnumerable<ClaimDocumentType>> LoadMetadata(IEnumerable<ClaimDocumentType> dtos)
		{
			if (dtos == null) throw new ArgumentNullException(nameof(dtos));
			if (dtos.Any())
			{
				var documentTypes = await _documentTypeService.GetAllDocumentTypes().ConfigureAwait(false);
				foreach(var dto in dtos)
				{
					dto.DocumentType = documentTypes.FirstOrDefault(x => x.Id == dto.DocumentTypeId);
				}
			}
			return dtos;
		}

		public async Task<ClaimDocumentType> GetClaimDocumentTypeById(int claimTypeId)
		{
			if (claimTypeId <= 0) throw new ArgumentException(nameof(claimTypeId));

			var dtos = await GetAllClaimDocumentTypes().ConfigureAwait(false);
			var dto = dtos.FirstOrDefault(b => b.Id == claimTypeId);
			if (dto == null) throw new NonExistingRecordCustomizedException();

			return dto;
		}

		#endregion
	}
}
