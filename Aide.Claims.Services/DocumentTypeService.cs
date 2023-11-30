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
	public interface IDocumentTypeService
	{
		Task<IEnumerable<DocumentType>> GetAllDocumentTypes();
		Task<DocumentType> GetDocumentTypeById(int documentTypeId);
		Task<IEnumerable<DocumentType>> GetDocumentTypeListByIds(int[] documentTypeIds);
	}

	public class DocumentTypeService: IDocumentTypeService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly ICacheService _cacheService;
		private const string _cacheKeyNameForDtoDocumentTypes = "Dto-List-DocumentType";

		#endregion

		#region Constructor

		public DocumentTypeService(IServiceProvider serviceProvider, ICacheService cacheService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<DocumentType>> GetAllDocumentTypes()
		{
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoDocumentTypes))
			{
				return _cacheService.Get<IEnumerable<DocumentType>>(_cacheKeyNameForDtoDocumentTypes);
			}
			//End Cache

			IEnumerable<DocumentType> dtos = new List<DocumentType>();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<document_type>(transientContext);
				var query = from document_type in repository.TableNoTracking select document_type;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return dtos;
				dtos = DocumentTypeMap.ToDto(entities);
			}

			//Begin Cache
			_cacheService.Set(_cacheKeyNameForDtoDocumentTypes, dtos);
			//End Cache

			return dtos;
		}

		public async Task<DocumentType> GetDocumentTypeById(int documentTypeId)
		{
			if (documentTypeId <= 0) throw new ArgumentException(nameof(documentTypeId));

			var dtos = await GetAllDocumentTypes().ConfigureAwait(false);
			var dto = dtos.FirstOrDefault(b => b.Id == documentTypeId);
			if (dto == null) throw new NonExistingRecordCustomizedException();

			return dto;
		}

		public async Task<IEnumerable<DocumentType>> GetDocumentTypeListByIds(int[] documentTypeIds)
		{
			if (documentTypeIds == null) throw new ArgumentException(nameof(documentTypeIds));

			var dtos = await GetAllDocumentTypes().ConfigureAwait(false);
			var dtosx = dtos.Where(x => documentTypeIds.Contains(x.Id));
			if (dtosx == null) throw new NonExistingRecordCustomizedException();

			return dtosx;
		}

		#endregion
	}
}
