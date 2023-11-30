using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Claims.Domain.Mapping;
using Aide.Claims.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Document = Aide.Claims.Domain.Objects.Document;

namespace Aide.Claims.Services
{
    public interface IDocumentService
	{
		Task<IEnumerable<Document>> GetDocumentListByIds(int[] documentIds);
		Task<Document> GetDocumentById(int DocumentId);
		Task InsertDocument(Document dto);
		Task UpdateDocument(Document dto);
		Task DeleteDocument(int DocumentId);
	}

	public class DocumentService : IDocumentService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;

		#endregion

		#region Constructor

		public DocumentService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<Document>> GetDocumentListByIds(int[] documentIds)
		{
			if (documentIds == null) throw new ArgumentException(nameof(documentIds));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<document>(transientContext);
				var query = from doc in repository.TableNoTracking
                            where documentIds.Contains(doc.document_id)
                            select doc;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (entities == null) throw new NonExistingRecordCustomizedException();

				var dtos = DocumentMap.ToDto(entities);
				return dtos;
			}
		}

		public async Task<Document> GetDocumentById(int documentId)
		{
			if (documentId == 0) throw new ArgumentException();

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<document>(transientContext);
				var entity = await repository.GetByIdAsync(documentId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				var dto = DocumentMap.ToDto(entity);
				return dto;
			}
		}

		public async Task InsertDocument(Document dto)
		{
			if (dto == null) throw new ArgumentNullException();

			dto.DateCreated = DateTime.UtcNow;
			dto.DateModified = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = DocumentMap.ToEntity(dto);
				var repository = new EfRepository<document>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.document_id;
			}
		}

		public async Task UpdateDocument(Document dto)
		{
			if (dto == null) throw new ArgumentNullException();
			if (dto.Id <= 0) throw new ArgumentException(nameof(dto.Id));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<document>(transientContext);
				var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				dto.DateModified = DateTime.UtcNow;
				entity = DocumentMap.ToEntity(dto, entity);
				await repository.UpdateAsync(entity).ConfigureAwait(false);
			}
		}

		public async Task DeleteDocument(int documentId)
		{
			if (documentId <= 0) throw new ArgumentException(nameof(documentId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<document>(transientContext);
				var entity = await repository.GetByIdAsync(documentId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				await repository.DeleteAsync(entity).ConfigureAwait(false);
			}
		}

		#endregion
	}
}
