using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Claims.Domain.Enumerations;
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
	public interface IClaimDocumentService
	{
		Task<IEnumerable<ClaimDocument>> GetClaimDocumentListByClaimIds(int[] claimIds);
		Task InsertClaimDocument(ClaimDocument dto);
		Task DeleteClaimDocumentByClaimIdAndDocumentTypeId(int claimId, int documentTypeId);
		Task<ClaimDocument> AttachDocument(int claimId, Document document, int documentTypeId, bool overwrite, int sortPriority, int groupId);
	}

	public class ClaimDocumentService : IClaimDocumentService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly IDocumentTypeService _documentTypeService;
		private readonly IDocumentService _documentService;

		#endregion

		#region Constructor

		public ClaimDocumentService(IServiceProvider serviceProvider, IDocumentTypeService documentTypeService, IDocumentService documentService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_documentTypeService = documentTypeService ?? throw new ArgumentNullException(nameof(documentTypeService));
			_documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<ClaimDocument>> GetClaimDocumentListByClaimIds(int[] claimIds)
		{
			if (claimIds == null) throw new ArgumentNullException(nameof(claimIds));

			IEnumerable<ClaimDocument> dtos;
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
                var repository = new EfRepository<claim_document>(transientContext);
				var query = from claim_document in repository.TableNoTracking
							where claimIds.Contains(claim_document.claim_id)
							select claim_document;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (entities == null) throw new NonExistingRecordCustomizedException();
				dtos = ClaimDocumentMap.ToDto(entities);
			}

			dtos = await LoadMetadata(dtos.ToList()).ConfigureAwait(false);
			return dtos;
		}

		private async Task<IEnumerable<ClaimDocument>> LoadMetadata(List<ClaimDocument> dtos)
		{
			if (dtos == null) throw new ArgumentNullException(nameof(dtos));
			if (dtos.Any())
			{
				// Document Type
				var documentTypeIds = dtos.Select(x => x.DocumentTypeId).ToArray();
				var documentTypes = await _documentTypeService.GetDocumentTypeListByIds(documentTypeIds).ConfigureAwait(false);

				// Document
				var documentIds = dtos.Select(x => x.DocumentId).ToArray();
				var documents = await _documentService.GetDocumentListByIds(documentIds).ConfigureAwait(false);

				// Attach metadata
				foreach(var dto in dtos)
				{
					dto.DocumentType = documentTypes.SingleOrDefault(x => x.Id == dto.DocumentTypeId);
					dto.Document = documents.SingleOrDefault(x => x.Id == dto.DocumentId);
				};
			}
			return dtos;
		}

		public async Task InsertClaimDocument(ClaimDocument dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			dto.DateCreated = DateTime.UtcNow;
			dto.DateModified = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = ClaimDocumentMap.ToEntity(dto);
				var repository = new EfRepository<claim_document>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.claim_document_id;
			}
		}

		public async Task DeleteClaimDocumentByClaimIdAndDocumentTypeId(int claimId, int documentTypeId)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));
			if (documentTypeId <= 0) throw new ArgumentException(nameof(documentTypeId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_document>(transientContext);
				var query = from claim_document in repository.Table
							where claim_document.claim_id == claimId && claim_document.claim_document_type_id == documentTypeId
							select claim_document;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (entities == null) throw new NonExistingRecordCustomizedException();

				foreach (var entity in entities.ToList())
				{
					await repository.DeleteAsync(entity).ConfigureAwait(false);
				}
			}
		}

		public async Task<ClaimDocument> AttachDocument(int claimId, Document document, int documentTypeId, bool overwrite, int sortPriority, int groupId)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));
			if (document == null) throw new ArgumentNullException(nameof(document));
			if (documentTypeId <= 0) throw new ArgumentException(nameof(documentTypeId));
			if (sortPriority <= -1) throw new ArgumentException(nameof(sortPriority));
			if (groupId <= -1) throw new ArgumentException(nameof(groupId));

			// Insert new Document
			await _documentService.InsertDocument(document).ConfigureAwait(false);

			if (overwrite)
			{
				// Delete previous ClaimDocument, if any, but don't delete the physical file yet.
				// The physical file should be deleted later in an automated cleansing process.
				try
				{
					await DeleteClaimDocumentByClaimIdAndDocumentTypeId(claimId, documentTypeId).ConfigureAwait(false);
				}
				catch (NonExistingRecordCustomizedException) { }
			}

			var newSortPriority = sortPriority;
			if (sortPriority == -1)
			{
				// Auto-calculate the next sort priority
			}

			// Insert new ClaimDocument
			var newClaimDocument = new ClaimDocument
			{
				ClaimId = claimId,
				DocumentTypeId = documentTypeId,
				DocumentId = document.Id,
				SortPriority = newSortPriority,
				GroupId = groupId,
				StatusId = EnumClaimDocumentStatusId.Completed
			};
			await InsertClaimDocument(newClaimDocument).ConfigureAwait(false);
			newClaimDocument.Document = document;
			return newClaimDocument;
		}

		#endregion
	}
}
