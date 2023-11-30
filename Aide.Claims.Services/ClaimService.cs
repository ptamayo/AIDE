using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Aide.Core.Interfaces;
using Aide.Claims.Domain.Enumerations;
using Aide.Claims.Domain.Extensions;
using Aide.Claims.Domain.Mapping;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClaimOrder = Aide.Claims.Domain.Objects.Claim;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace Aide.Claims.Services
{
	public interface IClaimService
	{
		Task<IPagedResult<ClaimOrder>> GetAllClaims(IPagingSettings pagingSettings, ClaimService.Filters filters, EnumViewDetail viewDetail);
		Task<ClaimOrder> GetClaimById(int claimId);
		Task<ClaimOrder> GetClaimById(int claimId, EnumViewDetail viewDetail);
		Task InsertClaim(ClaimOrder dto);
		Task UpdateClaim(ClaimOrder dto);
		Task<ClaimOrder> UpdateStatus(int claimId, int statusId);
		Task DeleteClaim(int claimId);
		Task SignClaim(ClaimSignature signature);
		Task<ClaimSignature> GetSignatureByClaimId(int claimId);
		Task<double> RemoveStaledOrders(double thresholdInHours);
		Task<bool> ExternalOrderNumberExists(string externalOrderNumber, int claimId);
	}

	public class ClaimService : IClaimService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private readonly IClaimTypeService _claimTypeService;
		private readonly IStoreService _storeService;
		private readonly IInsuranceCompanyService _insuranceCompanyService;
		private readonly IInsuranceProbatoryDocumentService _insuranceProbatoryDocumentService;
		private readonly IClaimProbatoryDocumentService _claimProbatoryDocumentService;
		private readonly IClaimDocumentService _claimDocumentService;
		private readonly IClaimDocumentTypeService _claimDocumentTypeService;
		private readonly IClaimSignatureService _claimSignatureService;
		private readonly IUserService _userService;
		private readonly ClaimServiceConfig _claimServiceConfig;
		private readonly IDocumentTypeService _documentTypeService;
		private readonly IProbatoryDocumentService _probatoryDocumentService;
		/// <summary>
		/// These are the groups of documents that are configured at insurance company level per claim/service type
		/// 1 = Admin Docs, 2 = Pictures, 4 = TPA Docs, 5 = Signature
		/// </summary>
		//private readonly ICollection<int> HeaderLevelDocumentGroupIds = new int[] { 1, 2, 4, 5 };
		/// <summary>
		/// Same explanation above, also these group will repeat the list of documents per item on order
		/// 3 = Pict x Item
		/// </summary>
		private readonly ICollection<int> ItemLevelDocumentGroupIds = new int[] { 3 };

		#endregion

		#region Constructor

		public ClaimService(IServiceProvider serviceProvider,
			IClaimTypeService claimTypeService,
			IStoreService storeService,
			IInsuranceCompanyService insuranceCompanyService,
			IInsuranceProbatoryDocumentService insuranceProbatoryDocumentService,
			IClaimProbatoryDocumentService claimProbatoryDocumentService,
			IClaimDocumentService claimDocumentService,
			IClaimDocumentTypeService claimDocumentTypeService,
			IClaimSignatureService claimSignatureService,
			IUserService userService,
			ClaimServiceConfig claimServiceConfig,
			IDocumentTypeService documentTypeService,
			IProbatoryDocumentService probatoryDocumentService)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_claimTypeService = claimTypeService ?? throw new ArgumentNullException(nameof(claimTypeService));
			_storeService = storeService ?? throw new ArgumentNullException(nameof(storeService));
			_insuranceCompanyService = insuranceCompanyService ?? throw new ArgumentNullException(nameof(insuranceCompanyService));
			_insuranceProbatoryDocumentService = insuranceProbatoryDocumentService ?? throw new ArgumentNullException(nameof(insuranceProbatoryDocumentService));
			_claimProbatoryDocumentService = claimProbatoryDocumentService ?? throw new ArgumentNullException(nameof(claimProbatoryDocumentService));
			_claimDocumentService = claimDocumentService ?? throw new ArgumentNullException(nameof(claimDocumentService));
			_claimDocumentTypeService = claimDocumentTypeService ?? throw new ArgumentNullException(nameof(claimDocumentTypeService));
			_claimSignatureService = claimSignatureService ?? throw new ArgumentNullException(nameof(claimSignatureService));
			_userService = userService ?? throw new ArgumentNullException(nameof(userService));
			_claimServiceConfig = claimServiceConfig ?? throw new ArgumentNullException(nameof(claimServiceConfig));
			_documentTypeService = documentTypeService ?? throw new ArgumentNullException(nameof(documentTypeService));
			_probatoryDocumentService = probatoryDocumentService ?? throw new ArgumentNullException(nameof(probatoryDocumentService));
		}

		#endregion

		#region Methods

		public async Task<IPagedResult<ClaimOrder>> GetAllClaims(IPagingSettings pagingSettings, ClaimService.Filters filters, EnumViewDetail viewDetail)
		{
			if (pagingSettings == null) throw new ArgumentNullException(nameof(pagingSettings));
			if (pagingSettings.PageNumber < 1 || pagingSettings.PageSize < 1) throw new ArgumentException(nameof(pagingSettings));
			if (filters == null) throw new ArgumentNullException(nameof(filters));
			IPagedResult<ClaimOrder> page;
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var query = await GetClaimsQuery(filters, transientContext).ConfigureAwait(false);
				var p = await EfRepository<claim>.PaginateAsync(pagingSettings, query).ConfigureAwait(false);
				if (!p.Results.Any()) return null;
				page = ClaimMap.ToDto(p);
			}
			if (page.Results.Any())
			{
				await LoadMetadata(page.Results.ToList(), viewDetail).ConfigureAwait(false);
			}
			return page;
		}

		private async Task<IQueryable<claim>> GetClaimsQuery(ClaimService.Filters filters, AideDbContext transientContext)
        {
			var repository = new EfRepository<claim>(transientContext);

			var query = (from x in repository.TableNoTracking
						 select x);

			// Filter by Insurance Company
			if (filters.DefaultInsuranceCompanyId.Any())
			{
				query = query.Where(x => filters.DefaultInsuranceCompanyId.Contains(x.insurance_company_id));
			}
            else if (filters.InsuranceCompanyId.HasValue && filters.InsuranceCompanyId.Value != 0)
            {
                query = query.Where(x => x.insurance_company_id == filters.InsuranceCompanyId);
            }
            // Filter by Store
            if (!string.IsNullOrWhiteSpace(filters.StoreName))
            {
				// Join to store/workshop
				// Notice this is a collection so that it needs RegexOptions.IgnoreCase
				var stores = await _storeService.GetAllStores().ConfigureAwait(false);
				var storeIds = stores.Where(x => $"{x.SAPNumber}-{x.Name}".Contains(filters.StoreName, StringComparison.OrdinalIgnoreCase))
									 .Select(s => s.Id)
									 .ToList();
				if (storeIds.Any())
                {
					query = query.Where(x => storeIds.Contains(x.store_id));
				}
			}
            if (filters.DefaultStoreId.Any())
			{
				query = query.Where(x => filters.DefaultStoreId.Contains(x.store_id));
			}
			// Filter by Keyword
			if (!string.IsNullOrWhiteSpace(filters.Keywords))
			{
				// Notice the keywords are converted to lowercase. Also there's no need to apply RegexOptions.IgnoreCase.
				// This is because the search will be performed against an EF Model.
				// See InsuranceCompanyService.GetAllClaims(... for an example of a different implementation.
				var keywords = filters.Keywords.EscapeRegexSpecialChars().ToLower().Split(' ');
				var regex = new Regex(string.Join("|", keywords));
				var regexString = regex.ToString();

				// Join to insurance company
				// Notice this is a collection so that it needs RegexOptions.IgnoreCase
				var insuranceCompanies = await _insuranceCompanyService.GetAllInsuranceCompanies().ConfigureAwait(false);
				var insuranceCompanyIds = insuranceCompanies.Where(x => Regex.IsMatch(x.Name, regexString, RegexOptions.IgnoreCase))
															.Select(ic => ic.Id)
															.ToList();
				// Join to store/workshop
				// Notice this is a collection so that it needs RegexOptions.IgnoreCase
				var stores = await _storeService.GetAllStores().ConfigureAwait(false);
				var storeIds = stores.Where(x => Regex.IsMatch(x.Name, regexString, RegexOptions.IgnoreCase))
									 .Select(s => s.Id)
									 .ToList();
				// Join to claim type
				var claimTypeRepository = new EfRepository<claim_type>(transientContext);

				query = from x in query
						join ct in claimTypeRepository.TableNoTracking on x.claim_type_id equals ct.claim_type_id
						where 1 == 1 &&
							(Regex.IsMatch(x.policy_number, regexString) ||
							Regex.IsMatch(x.report_number, regexString) ||
							Regex.IsMatch(x.claim_number, regexString) ||
							Regex.IsMatch(x.external_order_number, regexString) ||
							Regex.IsMatch(x.customer_full_name, regexString) ||
							Regex.IsMatch(ct.claim_type_name, regexString) ||
							(insuranceCompanyIds.Any() && insuranceCompanyIds.Contains(x.insurance_company_id)) ||
							(storeIds.Any() && storeIds.Contains(x.store_id)))
						select x;
			}
			// Filter by Status
			if (filters.StatusId.HasValue && filters.StatusId.Value != (int)EnumClaimStatusId.Unknown)
			{
				query = query.Where(x => x.claim_status_id == filters.StatusId.Value);
			}
			// Filter by Service Type
			if (filters.ServiceTypeId.HasValue && filters.ServiceTypeId.Value != (int)EnumClaimTypeId.Unknown)
            {
				query = query.Where(x => x.claim_type_id == filters.ServiceTypeId);
            }
			// Filter by Start Date
			if (filters.StartDate.HasValue)
            {
				query = query.Where(x => x.date_created >= filters.StartDate);
            }
			// Filter by End Date
			if (filters.EndDate.HasValue)
            {
				var endDate = filters.EndDate.Value.AddDays(1);
				query = query.Where(x => x.date_created < endDate);
            }

			// Order by date_created asc
			query = query.OrderBy(x => x.date_created);

			return query;
        }

		public async Task<ClaimOrder> GetClaimById(int claimId)
		{
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			return await this.GetClaimById(claimId, EnumViewDetail.Extended).ConfigureAwait(false);
		}

		public async Task<ClaimOrder> GetClaimById(int claimId, EnumViewDetail viewDetail)
		{
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));

			var dto = new ClaimOrder();
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim>(transientContext);
				var entity = await repository.GetByIdAsync(claimId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();
				dto = ClaimMap.ToDto(entity);
			}

			await LoadMetadata(dto, viewDetail).ConfigureAwait(false);
			return dto;
		}

		private async Task<IEnumerable<ClaimOrder>> LoadMetadata(List<ClaimOrder> dtos, EnumViewDetail viewDetail)
		{
			if (dtos == null) throw new ArgumentNullException(nameof(dtos));
			if (dtos.Any())
			{
				// Claim Type
				var claimTypes = await _claimTypeService.GetAllClaimTypes().ConfigureAwait(false);

				// Store
				var storeIds = dtos.Select(x => x.StoreId).Distinct().ToArray();
				var stores = await _storeService.GetStoreListByStoreIds(storeIds).ConfigureAwait(false);

				// Insurance Company
				var insuranceCompanyIds = dtos.Select(x => x.InsuranceCompanyId).Distinct().ToArray();
				var insuranceCompanies = await _insuranceCompanyService.GetInsuranceCompanyListByInsuranceCompanyIds(insuranceCompanyIds).ConfigureAwait(false);

				// Created By User
				var createdByUserIds = dtos.Select(x => x.CreatedByUserId).Distinct().ToArray();
				var createdByUsers = await _userService.GetUserListByUserIds(createdByUserIds).ConfigureAwait(false);

				// Probatory Document and Document
				int[] claimIds;
				IEnumerable<ClaimProbatoryDocument> claimProbatoryDocuments = new List<ClaimProbatoryDocument>();
				IEnumerable<ClaimDocument> claimDocuments = new List<ClaimDocument>();
				if (viewDetail == EnumViewDetail.Extended)
				{
					claimIds = dtos.Select(x => x.Id).ToArray();
					claimProbatoryDocuments = await _claimProbatoryDocumentService.GetClaimProbatoryDocumentListByClaimIds(claimIds).ConfigureAwait(false);
					claimDocuments = await _claimDocumentService.GetClaimDocumentListByClaimIds(claimIds).ConfigureAwait(false);
				}

				// Attach metadata
				foreach(var dto in dtos)
				{
					dto.ClaimType = claimTypes.SingleOrDefault(x => x.Id == (int)dto.ClaimTypeId);
					var store = stores.SingleOrDefault(x => x.Id == dto.StoreId);
					if (store != null)
					{
                        dto.Store = store.ToClaimStore();
                    }
					var insuranceCompany = insuranceCompanies.SingleOrDefault(x => x.Id == dto.InsuranceCompanyId);
					if (insuranceCompany != null)
					{
                        dto.InsuranceCompany = insuranceCompany.ToClaimInsuranceCompany();
                    }
					var user = createdByUsers.SingleOrDefault(x => x.Id == dto.CreatedByUserId);
					if (user != null)
					{
                        dto.CreatedByUser = user.ToClaimCreatedByUser();
                    }
					if (viewDetail == EnumViewDetail.Extended)
					{
						dto.ClaimProbatoryDocuments = claimProbatoryDocuments.Where(x => x.ClaimId == dto.Id).OrderBy(y => y.SortPriority).ToList();
						dto.ClaimDocuments = claimDocuments.Where(x => x.ClaimId == dto.Id).OrderBy(y => y.GroupId).ThenBy(z => z.SortPriority).ToList();
					}
				};
			}
			return dtos;
		}

		private async Task<ClaimOrder> LoadMetadata(ClaimOrder dto, EnumViewDetail viewDetail)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));
			var dtos = new List<ClaimOrder> { dto };
			await LoadMetadata(dtos, viewDetail).ConfigureAwait(false);
			return dto;
		}

		private async Task<ClaimDocument> AttachClaimDocumentDepositSlip(int claimId, int documentTypeId, int groupId, int sortPriority)
		{
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			if (documentTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(documentTypeId));
			if (groupId <= -1) throw new ArgumentException(nameof(groupId));
			if (sortPriority <= -1) throw new ArgumentException(nameof(sortPriority));
			var claimDocumentDepositSlip = new ClaimDocument
			{
				ClaimId = claimId,
				DocumentTypeId = documentTypeId,
				GroupId = groupId,
				SortPriority = sortPriority,
				StatusId = EnumClaimDocumentStatusId.InProcess
			};
			await _claimDocumentService.InsertClaimDocument(claimDocumentDepositSlip).ConfigureAwait(false);
			claimDocumentDepositSlip.DocumentType = await _documentTypeService.GetDocumentTypeById(documentTypeId).ConfigureAwait(false);
			return claimDocumentDepositSlip;
		}

		private async Task DeattachClaimDocumentDepositSlip(int claimId, int documentTypeId)
		{
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			if (documentTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(documentTypeId));
			await _claimDocumentService.DeleteClaimDocumentByClaimIdAndDocumentTypeId(claimId, documentTypeId).ConfigureAwait(false);
		}

		private async Task<ClaimProbatoryDocument> AttachClaimProbatoryDocumentDepositSlip(int claimId, int probatoryDocumentId, int groupId, int sortPriority)
		{
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			if (probatoryDocumentId <= 0) throw new ArgumentOutOfRangeException(nameof(probatoryDocumentId));
			if (groupId <= -1) throw new ArgumentException(nameof(groupId));
			if (sortPriority <= -1) throw new ArgumentException(nameof(sortPriority));
			var claimProbatoryDocumentDepositSlip = new ClaimProbatoryDocument
			{
				ClaimId = claimId,
				ProbatoryDocumentId = probatoryDocumentId,
				GroupId = groupId,
				SortPriority = sortPriority
			};
			await _claimProbatoryDocumentService.InsertClaimProbatoryDocument(claimProbatoryDocumentDepositSlip).ConfigureAwait(false);
			claimProbatoryDocumentDepositSlip.ProbatoryDocument = await _probatoryDocumentService.GetProbatoryDocumentById(probatoryDocumentId).ConfigureAwait(false);
			return claimProbatoryDocumentDepositSlip;
		}

		private async Task DeattachClaimProbatoryDocumentDepositSlip(int claimId, int probatoryDocumentId)
		{
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			if (probatoryDocumentId <= 0) throw new ArgumentOutOfRangeException(nameof(probatoryDocumentId));
			await _claimProbatoryDocumentService.DeleteClaimProbatoryDocumentByClaimIdAndProbatoryDocumentId(claimId, probatoryDocumentId).ConfigureAwait(false);
		}

		public async Task InsertClaim(ClaimOrder dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			dto.ClaimStatusId = EnumClaimStatusId.InProgress;
			dto.DateCreated = DateTime.UtcNow;
			dto.DateModified = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = ClaimMap.ToEntity(dto);
				var repository = new EfRepository<claim>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.claim_id;
			}

			// Attach list of default Document Type
			var claimDocuments = await AttachClaimDocuments(dto.Id).ConfigureAwait(false);

			// Insurance Probatory Documents
			var claimProbatoryDocuments = await BuildListOfInsuranceProbatoryDocuments(dto.InsuranceCompanyId, dto.ClaimTypeId, dto.Id, dto.ItemsQuantity).ConfigureAwait(false);

			// Verify if the claim service type requires the deposit slip
			var insuranceCompany = await _insuranceCompanyService.GetInsuranceCompanyById(dto.InsuranceCompanyId).ConfigureAwait(false);
			if (insuranceCompany.ClaimTypeSettings != null && insuranceCompany.ClaimTypeSettings.ContainsKey((int)dto.ClaimTypeId))
            {
				if (insuranceCompany.ClaimTypeSettings[(int)dto.ClaimTypeId].IsDepositSlipRequired)
				{
					// If the insured customer DOES HAVE deposit slip then attach the Probatory Document in the group of 1 = Admin Docs (Store)
					if (dto.HasDepositSlip)
					{
						var depositSlip = claimProbatoryDocuments.FirstOrDefault(x => x.ProbatoryDocumentId == _claimServiceConfig.DepositSlipConfig.StoreProbatoryDocument.ProbatoryDocumentId);
						if (depositSlip != null)
                        {
							depositSlip.GroupId = _claimServiceConfig.DepositSlipConfig.StoreProbatoryDocument.GroupId;
						}
					}
					else
					{
						// case else then attach the Probatory Document in the group of 4 = TPA Documents
						var depositSlip = claimProbatoryDocuments.FirstOrDefault(x => x.ProbatoryDocumentId == _claimServiceConfig.DepositSlipConfig.TPAProbatoryDocument.ProbatoryDocumentId);
						if (depositSlip != null)
						{
							depositSlip.GroupId = _claimServiceConfig.DepositSlipConfig.TPAProbatoryDocument.GroupId;
						}
					}
				}
			}

			// Attach Insurance Probatory Documents
			claimProbatoryDocuments = await AttachInsuranceProbatoryDocuments(claimProbatoryDocuments).ConfigureAwait(false);

			// And finally associate all documents to the claim that has just been created
			dto.ClaimDocuments = claimDocuments;
			dto.ClaimProbatoryDocuments = claimProbatoryDocuments;
		}

		/// <summary>
		/// These documents are inserted into table claim_document_type (see document_type)
		/// and every order will have them regardless of the claim type or insurance company
		/// </summary>
		/// <param name="claimId">claimId</param>
		/// <returns></returns>
		private async Task<IEnumerable<ClaimDocument>> AttachClaimDocuments(int claimId)
        {
			var claimDocuments = new List<ClaimDocument>();
			var claimDocumentTypes = await _claimDocumentTypeService.GetAllClaimDocumentTypes().ConfigureAwait(false);
			foreach (var claimDocumentType in claimDocumentTypes)
			{
				var claimDocument = claimDocumentType.ToClaimDocument();
				claimDocument.ClaimId = claimId;
				await _claimDocumentService.InsertClaimDocument(claimDocument).ConfigureAwait(false);
				claimDocuments.Add(claimDocument);
			}
			return claimDocuments;
		}

		/// <summary>
		/// These documents are inserted into table claim_probatory_document (see probatory_document and insurance_probatory_document)
		/// and the list may change depending of the claim type and insurance company
		/// </summary>
		/// <param name="insuranceCompanyId">insuranceCompanyId</param>
		/// <param name="claimTypeId">claimTypeId</param>
		/// <param name="claimId">claimId</param>
		/// <param name="itemsQuantity">itemsQuantity</param>
		/// <returns></returns>
		private async Task<IEnumerable<ClaimProbatoryDocument>> BuildListOfInsuranceProbatoryDocuments(int insuranceCompanyId, EnumClaimTypeId claimTypeId, int claimId, int itemsQuantity)
		{
			var insuranceProbatoryDocuments = await _insuranceProbatoryDocumentService.GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(insuranceCompanyId, claimTypeId).ConfigureAwait(false);
			var claimProbatoryDocuments = new List<ClaimProbatoryDocument>();
			// Attach documents at Claim Level (Header)
			var headerDocs = AttachInsuranceProbatoryDocumentsAtHeaderLevel(insuranceProbatoryDocuments, claimId);
			if (headerDocs.Any())
			{
				claimProbatoryDocuments.AddRange(headerDocs);
			}
			// Attach documents at Claim Item Level (Line Item)
			var itemDocs = AttachInsuranceProbatoryDocumentsAtItemLevel(insuranceProbatoryDocuments, claimId, itemsQuantity);
			if (itemDocs.Any())
			{
				claimProbatoryDocuments.AddRange(itemDocs);
			}
			// Return
			return claimProbatoryDocuments;
		}

		private async Task<IEnumerable<ClaimProbatoryDocument>> AttachInsuranceProbatoryDocuments(IEnumerable<ClaimProbatoryDocument> claimProbatoryDocuments)
        {
			// Persist and return
			return await _claimProbatoryDocumentService.InsertClaimProbatoryDocuments(claimProbatoryDocuments).ConfigureAwait(false);
        }

		/// <summary>
		/// Build a list of probatory documents at claim header level
		/// </summary>
		/// <param name="insuranceProbatoryDocuments"></param>
		/// <param name="claimId"></param>
		/// <returns></returns>
		private IEnumerable<ClaimProbatoryDocument> AttachInsuranceProbatoryDocumentsAtHeaderLevel(IEnumerable<InsuranceProbatoryDocument> insuranceProbatoryDocuments, int claimId)
		{
			var headerLevelDocs = insuranceProbatoryDocuments.Where(x => !ItemLevelDocumentGroupIds.Contains(x.GroupId));
			var claimProbatoryDocuments = new List<ClaimProbatoryDocument>();
			foreach (var insuranceProbatoryDocument in headerLevelDocs)
			{
				var claimProbatoryDocument = insuranceProbatoryDocument.ToClaimProbatoryDocument(claimId);
				claimProbatoryDocuments.Add(claimProbatoryDocument);
			}
			//claimProbatoryDocuments.All(x => { x.ClaimId = dto.Id; return true; });
			return claimProbatoryDocuments;
		}

		/// <summary>
		/// Build a list of probatory documents at claim item level
		/// </summary>
		/// <param name="insuranceProbatoryDocuments">insuranceProbatoryDocuments</param>
		/// <param name="claimId">claimId</param>
		/// <param name="itemsQuantityVariance">The variance only</param>
		/// <param name="currentItemsQuantity">The current items quantity on order</param>
		/// <returns></returns>
		private IEnumerable<ClaimProbatoryDocument> AttachInsuranceProbatoryDocumentsAtItemLevel(IEnumerable<InsuranceProbatoryDocument> insuranceProbatoryDocuments, int claimId, int itemsQuantityVariance, int currentItemsQuantity = 0)
		{
			var itemLevelDocs = insuranceProbatoryDocuments.Where(x => ItemLevelDocumentGroupIds.Contains(x.GroupId));
			var claimProbatoryDocuments = new List<ClaimProbatoryDocument>();
			for (var claimItemId = currentItemsQuantity + 1; claimItemId <= currentItemsQuantity + itemsQuantityVariance; claimItemId++)
			{
				foreach (var insuranceProbatoryDocument in itemLevelDocs)
				{
					var claimProbatoryDocument = insuranceProbatoryDocument.ToClaimItemProbatoryDocument(claimId, claimItemId);
					claimProbatoryDocuments.Add(claimProbatoryDocument);
				}
			}
			return claimProbatoryDocuments;
		}

		/// <summary>
		/// Build a list of probatory documents at claim item level AND persist to database
		/// </summary>
		/// <param name="insuranceCompanyId">insuranceCompanyId</param>
		/// <param name="claimTypeId">claimTypeId</param>
		/// <param name="claimId">claimId</param>
		/// <param name="itemsQuantityVariance">The variance only</param>
		/// <param name="currentItemsQuantity">The current items quantity on order</param>
		/// <returns></returns>
		private async Task<IEnumerable<ClaimProbatoryDocument>> AttachInsuranceProbatoryDocumentsAtItemLevel(int insuranceCompanyId, EnumClaimTypeId claimTypeId, int claimId, int itemsQuantityVariance, int currentItemsQuantity)
        {
			var insuranceProbatoryDocuments = await _insuranceProbatoryDocumentService.GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(insuranceCompanyId, claimTypeId).ConfigureAwait(false);
			var claimProbatoryDocuments = AttachInsuranceProbatoryDocumentsAtItemLevel(insuranceProbatoryDocuments, claimId, itemsQuantityVariance, currentItemsQuantity);
			// Persist and return
			return await _claimProbatoryDocumentService.InsertClaimProbatoryDocuments(claimProbatoryDocuments).ConfigureAwait(false);
		}

		public async Task UpdateClaim(ClaimOrder dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));
			if (dto.Id <= 0) throw new ArgumentOutOfRangeException(nameof(dto.Id));

			var currentItemsQuantity = 0;
			var itemsQuantityVariance = 0;
			var hasItemsQuantityValueChanged = false;
			var hasDepositSlipValueChanged = false;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim>(transientContext);
				var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				// Put here any fields you don't have an input control in the UI and you don't want it's value getting set to null.
				dto.Source = entity.source;
				dto.DateCreated = entity.date_created;
				
				dto.DateModified = DateTime.UtcNow;

				// Verify if the value on items quantity has changed
				hasItemsQuantityValueChanged = dto.ItemsQuantity != entity.items_quantity;
				currentItemsQuantity = entity.items_quantity;
				itemsQuantityVariance = dto.ItemsQuantity - entity.items_quantity;

				// Verify if the value on deposit slip has changed
				hasDepositSlipValueChanged = dto.HasDepositSlip != entity.has_deposit_slip;

				entity = ClaimMap.ToEntity(dto, entity);
				await repository.UpdateAsync(entity).ConfigureAwait(false);
			}

			// Load metadata
			dto = await LoadMetadata(dto, EnumViewDetail.Extended).ConfigureAwait(false);

			// If the value on items quantity has changed then add or remove the probatory documents accordingly
			if (hasItemsQuantityValueChanged)
            {
				// If the quantity of items increased
				if (itemsQuantityVariance > 0)
                {
					var newClaimItemProbatoryDocs = await AttachInsuranceProbatoryDocumentsAtItemLevel(dto.InsuranceCompanyId, dto.ClaimTypeId, dto.Id, itemsQuantityVariance, currentItemsQuantity).ConfigureAwait(false);
					dto.ClaimProbatoryDocuments = dto.ClaimProbatoryDocuments.Concat(newClaimItemProbatoryDocs);
                }
				else // If the quantity of items decreased
				{
					// Notice that in the operation below the second parameter will always be negative so that the substraction requires an addition
					var fromClaimItemId = currentItemsQuantity + itemsQuantityVariance;
					var probatoryDocIdsForDeletion = dto.ClaimProbatoryDocuments
						.Where(x => ItemLevelDocumentGroupIds.Contains(x.GroupId) && x.ClaimItemId > fromClaimItemId)
						.Select(s => s.Id)
						.ToArray();

					if (probatoryDocIdsForDeletion.Any())
					{
						// Remove from database
						await _claimProbatoryDocumentService.DeleteClaimProbatoryDocumentByIds(probatoryDocIdsForDeletion).ConfigureAwait(false);
						// Remove from DTO
						dto.ClaimProbatoryDocuments = dto.ClaimProbatoryDocuments
							.Where(x => !(ItemLevelDocumentGroupIds.Contains(x.GroupId) && x.ClaimItemId > fromClaimItemId));
					}
				}
			}

			// If the value on deposit slip has changed then determine where the document should be attached
			if (dto.IsDepositSlipRequired && hasDepositSlipValueChanged)
			{
				// Load all documents
				dto = await LoadMetadata(dto, EnumViewDetail.Extended).ConfigureAwait(false);
				var claimProbatoryDocuments = dto.ClaimProbatoryDocuments.ToList();
				var claimDocuments = dto.ClaimDocuments.ToList();

				// Deattach from the group 4 = TPA Documents and Attach to group 1 = Administrative Documents
				if (dto.HasDepositSlip)
				{
					try
					{
						await DeattachClaimProbatoryDocumentDepositSlip(dto.Id, _claimServiceConfig.DepositSlipConfig.TPAProbatoryDocument.ProbatoryDocumentId).ConfigureAwait(false);
						var tpaProbatoryDocumentDepositSlip = claimProbatoryDocuments.FirstOrDefault(x => x.ProbatoryDocumentId == _claimServiceConfig.DepositSlipConfig.TPAProbatoryDocument.ProbatoryDocumentId);
						if (tpaProbatoryDocumentDepositSlip != null)
						{
							claimProbatoryDocuments.Remove(tpaProbatoryDocumentDepositSlip);
						}
						dto.ClaimProbatoryDocuments = claimProbatoryDocuments;
					}
					catch (NonExistingRecordCustomizedException) { }
					catch { throw; }

					var storeProbatoryDocumentDepositSlip = await AttachClaimProbatoryDocumentDepositSlip(dto.Id,
					_claimServiceConfig.DepositSlipConfig.StoreProbatoryDocument.ProbatoryDocumentId,
					_claimServiceConfig.DepositSlipConfig.StoreProbatoryDocument.GroupId,
					_claimServiceConfig.DepositSlipConfig.StoreProbatoryDocument.SortPriority).ConfigureAwait(false);
					claimProbatoryDocuments.Insert(0, storeProbatoryDocumentDepositSlip);
					dto.ClaimProbatoryDocuments = claimProbatoryDocuments;
				}
				else
				{
					// Deattach from the group 1 = Administrative Documents and Attach to group 4 = TPA Documents
					try
					{
						await DeattachClaimProbatoryDocumentDepositSlip(dto.Id, _claimServiceConfig.DepositSlipConfig.StoreProbatoryDocument.ProbatoryDocumentId).ConfigureAwait(false);
						var storeProbatoryDocumentDepositSlip = claimProbatoryDocuments.FirstOrDefault(x => x.ProbatoryDocumentId == _claimServiceConfig.DepositSlipConfig.StoreProbatoryDocument.ProbatoryDocumentId);
						if (storeProbatoryDocumentDepositSlip != null)
						{
							claimProbatoryDocuments.Remove(storeProbatoryDocumentDepositSlip);
						}
						dto.ClaimProbatoryDocuments = claimProbatoryDocuments;
					}
					catch (NonExistingRecordCustomizedException) { }
					catch { throw; }

					var tpaProbatoryDocumentDepositSlip = await AttachClaimProbatoryDocumentDepositSlip(dto.Id,
					_claimServiceConfig.DepositSlipConfig.TPAProbatoryDocument.ProbatoryDocumentId,
					_claimServiceConfig.DepositSlipConfig.TPAProbatoryDocument.GroupId,
					_claimServiceConfig.DepositSlipConfig.TPAProbatoryDocument.SortPriority).ConfigureAwait(false);
					claimProbatoryDocuments.Insert(0, tpaProbatoryDocumentDepositSlip);
					dto.ClaimProbatoryDocuments = claimProbatoryDocuments;
				}
			}
		}

		public async Task<ClaimOrder> UpdateStatus(int claimId, int statusId)
        {
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			if (statusId <= 0) throw new ArgumentOutOfRangeException(nameof(statusId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim>(transientContext);
				var entity = await repository.GetByIdAsync(claimId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				entity.date_modified = DateTime.UtcNow;
				entity.claim_status_id = statusId;

				await repository.UpdateAsync(entity).ConfigureAwait(false);

				return ClaimMap.ToDto(entity);
            }
		}

		public async Task DeleteClaim(int claimId)
		{
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim>(transientContext);
				var entity = await repository.GetByIdAsync(claimId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				await repository.DeleteAsync(entity).ConfigureAwait(false);
			}
		}

		public async Task SignClaim(ClaimSignature signature)
		{
			if (signature == null) throw new ArgumentNullException(nameof(signature));
			await _claimSignatureService.InsertClaimSignature(signature).ConfigureAwait(false);
		}

		public async Task<ClaimSignature> GetSignatureByClaimId(int claimId)
		{
			if (claimId <= 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			var signature = await _claimSignatureService.GetClaimSignatureByClaimId(claimId).ConfigureAwait(false);
			return signature;
		}

		/// <summary>
		/// This method is NOT destructive, it won't delete any claims/orders but update the status to Cancelled.
		/// </summary>
		/// <param name="thresholdInHours">thresholdInHours</param>
		/// <returns>The total count of orders that were updated</returns>
		public async Task<double> RemoveStaledOrders(double thresholdInHours)
        {
			if (thresholdInHours <= 0) throw new ArgumentOutOfRangeException(nameof(thresholdInHours));

			var pagingSettings = new PagingSettings
			{
				PageNumber = 1,
				PageSize = 1000
			};

			var thresholdDate = DateTime.UtcNow.AddHours(-thresholdInHours);
			double staledOrdersRemoved = 0;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
				var repository = new EfRepository<claim>(transientContext);
				var query = (from c in repository.Table
                             where c.claim_status_id == (int)EnumClaimStatusId.InProgress && c.date_created < thresholdDate
							 select c);

                // Apply exclusions, if any:
                var exclusions = _claimServiceConfig.StaleOrdersRemovalConfig.SourceExclusions;
                if (exclusions != null && exclusions.Any())
				{
					query = query.Where(x => !exclusions.Contains(x.source));
				}

				// Sorting the results:
				query = query.OrderBy(x => x.date_created);

				IPagedResult<claim> p = null;
				int affectedRows = 0;
				do
				{
					p = await EfRepository<claim>.PaginateAsync(pagingSettings, query).ConfigureAwait(false);
					if (p.Results.Any())
                    {
						staledOrdersRemoved += p.Results.Count();
						p.Results.ToList().ForEach(claim => claim.claim_status_id = (int)EnumClaimStatusId.Cancelled);
						affectedRows = await repository.UpdateAsync(p.Results).ConfigureAwait(false);
                    }
				} while (p != null && p.Results.Any() && affectedRows > 0);
			}
			return staledOrdersRemoved;
		}

		public async Task<bool> ExternalOrderNumberExists(string externalOrderNumber, int claimId)
        {
			// Dev Notes: Be aware that claimId = 0 is valid because it's related to new orders in the front-end that are not saved yet
			if (claimId < 0) throw new ArgumentOutOfRangeException(nameof(claimId));
			if (string.IsNullOrWhiteSpace(externalOrderNumber)) throw new ArgumentNullException(nameof(externalOrderNumber));
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim>(transientContext);
				var query = from claim in repository.TableNoTracking
							where claim.external_order_number == externalOrderNumber.Trim()
							select claim;
				if (claimId > 0)
                {
					// You need exclude the order ID from where this request is being made
					query = query.Where(claim => claim.claim_id != claimId);

				}
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (entities == null || !entities.Any())
                {
					return false;
				}
                else
                {
					return true;
                }
			}
		}

		#endregion

		#region Local classes

		public class Filters
		{
			public IEnumerable<int> DefaultInsuranceCompanyId { get; set; }
			public IEnumerable<int> DefaultStoreId { get; set; }
			public string Keywords { get; set; }
			public int? StatusId { get; set; }
			public string StoreName { get; set; }
			public int? ServiceTypeId { get; set; }
			public int? InsuranceCompanyId { get; set; }
			public DateTime? StartDate { get; set; }
			public DateTime? EndDate { get; set; }
		}

		public class ClaimServiceConfig
		{
			public DepositSlipConfig DepositSlipConfig { get; set; }
			public StaleOrdersRemovalConfig StaleOrdersRemovalConfig { get; set; }
        }

		public class DepositSlipConfig
		{
			public ProbatoryDocumentConfig TPAProbatoryDocument { get; set; }
			public ProbatoryDocumentConfig StoreProbatoryDocument { get; set; }
		}

		public class ProbatoryDocumentConfig
		{
			public int ProbatoryDocumentId { get; set; }
			public int GroupId { get; set; }
			public int SortPriority { get; set; }
		}

		public class StaleOrdersRemovalConfig
		{
			public string[] SourceExclusions { get; set; }
		}

        #endregion
    }
}
