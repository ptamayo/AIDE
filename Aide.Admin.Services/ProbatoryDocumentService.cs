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
	public interface IProbatoryDocumentService
	{
		Task<IEnumerable<ProbatoryDocument>> GetAllProbatoryDocuments();
		Task<IPagedResult<ProbatoryDocument>> GetAllProbatoryDocuments(IPagingSettings pagingSettings, ProbatoryDocumentService.Filters filters);
		Task<IEnumerable<ProbatoryDocument>> GetProbatoryDocumentListByIds(int[] probatoryDocumentIds);
		Task<ProbatoryDocument> GetProbatoryDocumentById(int probatoryDocumentId);
		Task InsertProbatoryDocument(ProbatoryDocument dto);
		Task UpdateProbatoryDocument(ProbatoryDocument dto);
	}

	public class ProbatoryDocumentService : IProbatoryDocumentService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly ICacheService _cacheService;
		private const string _cacheKeyNameForDtoProbatoryDocuments = "Dto-List-ProbatoryDocument";

		#endregion

		#region Constructor

		public ProbatoryDocumentService(IServiceProvider serviceProvider, ICacheService cacheService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
		}

		#endregion

		#region Methods

		public async Task<IEnumerable<ProbatoryDocument>> GetAllProbatoryDocuments()
		{
			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoProbatoryDocuments))
			{
				return _cacheService.Get<IEnumerable<ProbatoryDocument>>(_cacheKeyNameForDtoProbatoryDocuments);
			}
			//End Cache

			IEnumerable<ProbatoryDocument> dtos = new List<ProbatoryDocument>();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<probatory_document>(transientContext);
				var query = from probatory_document in repository.TableNoTracking select probatory_document;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return dtos;
				dtos = ProbatoryDocumentMap.ToDto(entities);
			}

			// Order the list of probatory documents before caching
			var orderedList = OrderProbatoryDocuments(dtos);

			//Begin Cache
			_cacheService.Set(_cacheKeyNameForDtoProbatoryDocuments, orderedList);
			//End Cache

			return orderedList;
		}

		public async Task<IPagedResult<ProbatoryDocument>> GetAllProbatoryDocuments(IPagingSettings pagingSettings, ProbatoryDocumentService.Filters filters)
		{
			if (pagingSettings == null) throw new ArgumentNullException(nameof(pagingSettings));
			if (pagingSettings.PageNumber < 1 || pagingSettings.PageSize < 1) throw new ArgumentException(nameof(pagingSettings));
			if (filters == null) throw new ArgumentNullException(nameof(filters));

			var result = await GetAllProbatoryDocuments().ConfigureAwait(false);
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

			var p = EfRepository<ProbatoryDocument>.Paginate(pagingSettings, result);
			if (!p.Results.Any()) return null;
			var pageResult = ProbatoryDocumentMap.ToDto(p);
			return pageResult;
		}

		public async Task<IEnumerable<ProbatoryDocument>> GetProbatoryDocumentListByIds(int[] probatoryDocumentIds)
		{
			if (probatoryDocumentIds == null) throw new ArgumentNullException(nameof(probatoryDocumentIds));
			var dtos = await GetAllProbatoryDocuments().ConfigureAwait(false);
			var result = dtos.Where(x => probatoryDocumentIds.Contains(x.Id)).ToList();
			return result;
		}

		//public IPagedResult<ProbatoryDocument> GetAllProbatoryDocuments(IPagingSettings pagingSettings, string orderByExpression)
		//{
		//	var entityPage = _repository.GetAll(pagingSettings, orderByExpression);
		//	if (entityPage == null) return null;

		//	var pageResult = ProbatoryDocumentMap.ToDto(entityPage);
		//	return pageResult;
		//}

		private IEnumerable<ProbatoryDocument> OrderProbatoryDocuments(IEnumerable<ProbatoryDocument> documents)
		{
			return documents.OrderBy(o => o.Name);
		}

		public async Task<ProbatoryDocument> GetProbatoryDocumentById(int probatoryDocumentId)
		{
			if (probatoryDocumentId <= 0) throw new ArgumentException(nameof(probatoryDocumentId));

			var dtos = await GetAllProbatoryDocuments().ConfigureAwait(false);
			var dto = dtos.FirstOrDefault(b => b.Id == probatoryDocumentId);
			if (dto == null) throw new NonExistingRecordCustomizedException();

			return dto;
		}

		public async Task InsertProbatoryDocument(ProbatoryDocument dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			dto.DateCreated = DateTime.UtcNow;
			dto.DateModified = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = ProbatoryDocumentMap.ToEntity(dto);
				var repository = new EfRepository<probatory_document>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.probatory_document_id;
			}

			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoProbatoryDocuments))
			{
				var dtos = await GetAllProbatoryDocuments().ConfigureAwait(false);
				var list = dtos.ToList();
				list.Add(dto);
				var orderedList = OrderProbatoryDocuments(list);
				_cacheService.Set(_cacheKeyNameForDtoProbatoryDocuments, orderedList);
			}
			//End Cache
		}

		public async Task UpdateProbatoryDocument(ProbatoryDocument dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));
			if (dto.Id <= 0) throw new ArgumentException(nameof(dto.Id));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<probatory_document>(transientContext);
				var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException(nameof(entity));

				dto.DateCreated = entity.date_created;
				dto.DateModified = DateTime.UtcNow;

				entity = ProbatoryDocumentMap.ToEntity(dto, entity);
				await repository.UpdateAsync(entity).ConfigureAwait(false);
			}

			//Begin Cache
			if (_cacheService.Exist(_cacheKeyNameForDtoProbatoryDocuments))
			{
				var dtos = await GetAllProbatoryDocuments().ConfigureAwait(false);
				var list = dtos.ToList();
				var currentDto = list.Find(li => li.Id == dto.Id);
				currentDto = ProbatoryDocumentMap.ToDto(dto, currentDto);
				var orderedList = OrderProbatoryDocuments(list);
				_cacheService.Set(_cacheKeyNameForDtoProbatoryDocuments, orderedList);
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
