using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Aide.Core.Interfaces;
using Moq;
using Moq.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aide.Admin.Services.UnitTests
{
	/// <summary>
	/// VERY IMPORTANT:
	/// If in the future the property of ProbatoryDocument is added to InsuranceCollageProbatoryDocument
	/// please visit the Unit Test on InsuranceProbatoryDocumentServiceTests and check how the metadata
	/// is validated in some of the test case scenarios
	/// </summary>
	[TestFixture]
	public class InsuranceCollageProbatoryDocumentServiceTests
	{
		private Mock<IServiceProvider> _serviceProvider;
		private Mock<ICacheService> _cacheService;
		private const string _cacheKeyNameForDtoInsuranceCollageProbatoryDocuments = "Dto-List-InsuranceCollageProbatoryDocument";
		private Mock<AideDbContext> _context;
		private IInsuranceCollageProbatoryDocumentService service;

		[SetUp]
		public void Setup()
		{
			_serviceProvider = new Mock<IServiceProvider>();
			_cacheService = new Mock<ICacheService>();
			_context = new Mock<AideDbContext>();
			service = new InsuranceCollageProbatoryDocumentService(_serviceProvider.Object, _cacheService.Object);
		}

		/// <summary>
		/// Setup the context.
		/// Need call this in scenarios that will query the database.
		/// IMPORTANT: The entity initialization must happen before calling this.
		/// </summary>
		private void SetupContext()
		{
			// For passing the DbSet from context to EfRepository
			_context.Setup(x => x.Set<insurance_collage_probatory_document>()).Returns(_context.Object.insurance_collage_probatory_document);
			// Initialize context scope
			_serviceProvider.Setup(x => x.GetService(typeof(AideDbContext))).Returns(_context.Object);
			// For initialization of context in EfRepository (it seems like this step is not needed)
			//_serviceProvider.Setup(x => x.GetService(typeof(DbContext))).Returns(_context.Object);
		}

		#region GetAllInsuranceCollageProbatoryDocuments

		[Test]
		public async Task GetAllInsuranceCollageProbatoryDocuments_WhenIsFirstTimeCall_ThenPullDataFromDbAndCacheIt()
		{
			#region Arrange

			// These are the mocked records from db
			var insurance_collage_probatory_document_entities = new List<insurance_collage_probatory_document>
			{
				// Collage #1: Contains 3 probatory documents
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 1,
					insurance_collage_id = 1,
					probatory_document_id = 1,
					sort_priority = 1
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 2,
					insurance_collage_id = 1,
					probatory_document_id = 2,
					sort_priority = 2
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 3,
					insurance_collage_id = 1,
					probatory_document_id = 3,
					sort_priority = 3
				},
				// Collage #2: Contains 2 probatory documents
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 4,
					insurance_collage_id = 2,
					probatory_document_id = 1,
					sort_priority = 1
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 5,
					insurance_collage_id = 2,
					probatory_document_id = 4,
					sort_priority = 2
				}
			};

			// When method is called by first time there's nothing in cache
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceCollageProbatoryDocuments))).Returns(false);
			// Initialize DbSet in context
			_context.Setup(x => x.insurance_collage_probatory_document).ReturnsDbSet(insurance_collage_probatory_document_entities);
			this.SetupContext();

			#endregion

			#region Act

			var result = await service.GetAllInsuranceCollageProbatoryDocuments().ConfigureAwait(false);

			#endregion

			#region Assert

			// Look for a cache entry
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments), Times.Once);
			// Since there's no cache then it won't try pull data from there
			_cacheService.Verify(v => v.Get(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments), Times.Never);
			// Initialize a db context
			_serviceProvider.Verify(v => v.GetService(typeof(AideDbContext)), Times.Once);
			// Pull data from db one time only
			_context.Verify(v => v.Set<insurance_collage_probatory_document>(), Times.Once);
			// ... and cache the data
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments, It.IsAny<List<InsuranceCollageProbatoryDocument>>()), Times.Once);
			// Verify the result is not null
			Assert.IsNotNull(result);
			// Verify the result has the same quantity of items as the number of rows in db
			Assert.AreEqual(result.Count(), insurance_collage_probatory_document_entities.Count());

			#endregion
		}

		[Test]
		public async Task GetAllInsuranceCollageProbatoryDocuments_WhenIsSecondTimeCallAndGoingForward_ThenPullDataFromCache()
		{
			#region Arrange

			// These are cached records (notice that it is an object type)
			IEnumerable<InsuranceCollageProbatoryDocument> cachedInsuranceCollageProbatoryDocuments = new List<InsuranceCollageProbatoryDocument>
			{
				// Collage #1: Contains 3 probatory documents
				new InsuranceCollageProbatoryDocument
				{
					Id = 1,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 1,
					SortPriority = 1
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 2,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 2,
					SortPriority = 2
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 3,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 3,
					SortPriority = 3
				},
				// Collage #2: Contains 2 probatory documents
				new InsuranceCollageProbatoryDocument
				{
					Id = 4,
					InsuranceCollageId = 2,
					ProbatoryDocumentId = 1,
					SortPriority = 1
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 5,
					InsuranceCollageId = 2,
					ProbatoryDocumentId = 4,
					SortPriority = 2
				}
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceCollageProbatoryDocuments))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<InsuranceCollageProbatoryDocument>>(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments)).Returns(cachedInsuranceCollageProbatoryDocuments);

			#endregion

			#region Act

			var result = await service.GetAllInsuranceCollageProbatoryDocuments().ConfigureAwait(false);

			#endregion

			#region Assert

			// Look for a cache entry
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments), Times.Once);
			// It pulls data from cache always
			_cacheService.Verify(v => v.Get<IEnumerable<InsuranceCollageProbatoryDocument>>(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments), Times.Once);
			// It NEVER initialize a db context scope
			_serviceProvider.Verify(v => v.GetService(typeof(AideDbContext)), Times.Never);
			// so that, it NEVER pulls data from db
			_context.Verify(v => v.Set<insurance_collage_probatory_document>(), Times.Never);
			// Verify the result is not null
			Assert.IsNotNull(result);
			// Verify the result has the same quantity of items as the number of rows in db
			Assert.AreEqual(result.Count(), cachedInsuranceCollageProbatoryDocuments.Count());

			#endregion
		}

		#endregion

		#region GetInsuranceCollageProbatoryDocumentListByCollageId

		// Pending ...

		#endregion

		#region UpsertInsuranceCollageProbatoryDocuments

		[Test]
		public void UpsertInsuranceCollageProbatoryDocuments_WhenTheListOfCollageDocumentsIsNull_ThenThrowArgumentNullException()
		{
			#region Arrange

			var collageId = 1;
			List<InsuranceCollageProbatoryDocument> collageDocuments = null;

			#endregion

			#region Act

			// NA

			#endregion

			#region Assert

			Assert.That(() => service.UpsertInsuranceCollageProbatoryDocuments(collageDocuments, collageId), Throws.Exception.TypeOf<ArgumentNullException>());

			#endregion
		}

		[Test]
		public void UpsertInsuranceCollageProbatoryDocuments_WhenCollageIdIsInvalid_ThenThrowArgumentException()
		{
			#region Arrange

			var collageId = 0;
			var emptyListOfCollageDocuments = new List<InsuranceCollageProbatoryDocument>
			{
				new InsuranceCollageProbatoryDocument
				{
					Id = collageId
				}
			};

			#endregion

			#region Act

			// NA

			#endregion

			#region Assert

			Assert.That(() => service.UpsertInsuranceCollageProbatoryDocuments(emptyListOfCollageDocuments, collageId), Throws.Exception.TypeOf<ArgumentException>());

			#endregion
		}

		[Test]
		public void UpsertInsuranceCollageProbatoryDocuments_WhenCollageIdOnAnyDocumentIsInvalid_ThenThrowArgumentException()
		{
			#region Arrange

			var collageId = 1;
			var emptyListOfCollageDocuments = new List<InsuranceCollageProbatoryDocument>
			{
				new InsuranceCollageProbatoryDocument
				{
					Id = 0 // Anything equals to 0 (zero) or below is invalid
				}
			};

			#endregion

			#region Act

			// NA

			#endregion

			#region Assert

			Assert.That(() => service.UpsertInsuranceCollageProbatoryDocuments(emptyListOfCollageDocuments, collageId), Throws.Exception.TypeOf<ArgumentException>());

			#endregion
		}

		[Test]
		public void UpsertInsuranceCollageProbatoryDocuments_WhenCollageIdOnAnyDocumentIsIncorrect_ThenThrowArgumentException()
		{
			#region Arrange

			var collageId = 1;
			var emptyListOfCollageDocuments = new List<InsuranceCollageProbatoryDocument>
			{
				new InsuranceCollageProbatoryDocument
				{
					Id = 2 // This is incorrect because should be 1
				}
			};

			#endregion

			#region Act

			// NA

			#endregion

			#region Assert

			Assert.That(() => service.UpsertInsuranceCollageProbatoryDocuments(emptyListOfCollageDocuments, collageId), Throws.Exception.TypeOf<ArgumentException>());

			#endregion
		}

		[Test]
		public async Task UpsertInsuranceCollageProbatoryDocuments_WhenRemovedDocuments_ThenApplyChangesToDatabase()
		{
			#region Arrange

			// These are the mocked records from cache
			IEnumerable<InsuranceCollageProbatoryDocument> cachedInsuranceCollageProbatoryDocuments = new List<InsuranceCollageProbatoryDocument>
			{
				// Collage #1: Contains 3 probatory documents
				new InsuranceCollageProbatoryDocument
				{
					Id = 1,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 1,
					SortPriority = 1
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 2,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 2,
					SortPriority = 2
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 3,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 3,
					SortPriority = 3
				},
				// Collage #2: Contains 2 probatory documents
				new InsuranceCollageProbatoryDocument
				{
					Id = 4,
					InsuranceCollageId = 2,
					ProbatoryDocumentId = 1,
					SortPriority = 1
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 5,
					InsuranceCollageId = 2,
					ProbatoryDocumentId = 4,
					SortPriority = 2
				}
			};

			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceCollageProbatoryDocuments))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<InsuranceCollageProbatoryDocument>>(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments)).Returns(cachedInsuranceCollageProbatoryDocuments);

			// These are the mocked records from db
			var insurance_collage_probatory_document_entities = new List<insurance_collage_probatory_document>
			{
				// Collage #1: Contains 3 probatory documents
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 1,
					insurance_collage_id = 1,
					probatory_document_id = 1,
					sort_priority = 1
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 2,
					insurance_collage_id = 1,
					probatory_document_id = 2,
					sort_priority = 2
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 3,
					insurance_collage_id = 1,
					probatory_document_id = 3,
					sort_priority = 3
				},
				// Collage #2: Contains 2 probatory documents
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 4,
					insurance_collage_id = 2,
					probatory_document_id = 1,
					sort_priority = 1
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 5,
					insurance_collage_id = 2,
					probatory_document_id = 4,
					sort_priority = 2
				}
			};

			// Initialize DbSet in context
			_context.Setup(x => x.insurance_collage_probatory_document).ReturnsDbSet(insurance_collage_probatory_document_entities);
			this.SetupContext();

			// The very last document was deleted from collage #1
			var probatoryDocumentIdDeletedFromCollage = 3;
			var collageId = 1;
			var newCollageDocuments = new List<InsuranceCollageProbatoryDocument>
			{
				new InsuranceCollageProbatoryDocument
				{
					Id = 1,
					InsuranceCollageId = collageId,
					ProbatoryDocumentId = 1,
					//SortPriority = 1 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 2,
					InsuranceCollageId = collageId,
					ProbatoryDocumentId = 2,
					//SortPriority = 2 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				}
			};

			#endregion

			#region Act

			await service.UpsertInsuranceCollageProbatoryDocuments(newCollageDocuments, collageId).ConfigureAwait(false);

			#endregion

			#region Assert

			// Delete the record from db
			var deletedEntity = _context.Object.insurance_collage_probatory_document.FirstOrDefault(entity => entity.insurance_collage_probatory_document_id == probatoryDocumentIdDeletedFromCollage && entity.insurance_collage_id == collageId);
			_context.Verify(v => v.insurance_collage_probatory_document.Remove(It.Is<insurance_collage_probatory_document>(a => a == deletedEntity)), Times.Once);
			// Delete the record from cache
			var updatedCache = cachedInsuranceCollageProbatoryDocuments.ToList();
			updatedCache.RemoveAll(x => x.ProbatoryDocumentId == probatoryDocumentIdDeletedFromCollage && x.InsuranceCollageId == collageId);
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments, updatedCache), Times.Once);

			#endregion
		}

		[Test]
		public async Task UpsertInsuranceCollageProbatoryDocuments_WhenUpdatedDocuments_ThenApplyChangesToDatabase()
		{
			#region Arrange

			// These are the mocked records from cache
			IEnumerable<InsuranceCollageProbatoryDocument> cachedInsuranceCollageProbatoryDocuments = new List<InsuranceCollageProbatoryDocument>
			{
				// Collage #1: Contains 3 probatory documents
				new InsuranceCollageProbatoryDocument
				{
					Id = 1,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 1,
					SortPriority = 1
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 2,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 2,
					SortPriority = 2
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 3,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 3,
					SortPriority = 3
				},
				// Collage #2: Contains 2 probatory documents
				new InsuranceCollageProbatoryDocument
				{
					Id = 4,
					InsuranceCollageId = 2,
					ProbatoryDocumentId = 1,
					SortPriority = 1
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 5,
					InsuranceCollageId = 2,
					ProbatoryDocumentId = 4,
					SortPriority = 2
				}
			};

			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceCollageProbatoryDocuments))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<InsuranceCollageProbatoryDocument>>(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments)).Returns(cachedInsuranceCollageProbatoryDocuments);

			// These are the mocked records from db
			var insurance_collage_probatory_document_entities = new List<insurance_collage_probatory_document>
			{
				// Collage #1: Contains 3 probatory documents
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 1,
					insurance_collage_id = 1,
					probatory_document_id = 1,
					sort_priority = 1
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 2,
					insurance_collage_id = 1,
					probatory_document_id = 2,
					sort_priority = 2
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 3,
					insurance_collage_id = 1,
					probatory_document_id = 3,
					sort_priority = 3
				},
				// Collage #2: Contains 2 probatory documents
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 4,
					insurance_collage_id = 2,
					probatory_document_id = 1,
					sort_priority = 1
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 5,
					insurance_collage_id = 2,
					probatory_document_id = 4,
					sort_priority = 2
				}
			};

			// Initialize DbSet in context
			_context.Setup(x => x.insurance_collage_probatory_document).ReturnsDbSet(insurance_collage_probatory_document_entities);
			this.SetupContext();

			// The items #1 and #2 have been inverted in the collage #1
			// IMPORTANT: The sortpriority is initialized in the back end and it will depend in the position they have in the request
			var collageId = 1;
			var updatedCollageDocuments = new List<InsuranceCollageProbatoryDocument>
			{
				new InsuranceCollageProbatoryDocument
				{
					Id = 2,
					InsuranceCollageId = collageId,
					ProbatoryDocumentId = 2,
					//SortPriority = 2 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 1,
					InsuranceCollageId = collageId,
					ProbatoryDocumentId = 1,
					//SortPriority = 1 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 3,
					InsuranceCollageId = collageId,
					ProbatoryDocumentId = 3,
					//SortPriority = 3 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				}
			};

			#endregion

			#region Act

			await service.UpsertInsuranceCollageProbatoryDocuments(updatedCollageDocuments, collageId).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify there aren't any records being deleted from db
			_context.Verify(v => v.insurance_collage_probatory_document.Remove(It.IsAny<insurance_collage_probatory_document>()), Times.Never);
			// Verify that items are updated in the database
			var item1 = updatedCollageDocuments.First();
			var item2 = updatedCollageDocuments[1];
			Assert.IsTrue(_context.Object.insurance_collage_probatory_document.Any(entity => entity.probatory_document_id == item1.ProbatoryDocumentId && entity.sort_priority == item1.SortPriority && entity.insurance_collage_id == item1.InsuranceCollageId));
			Assert.IsTrue(_context.Object.insurance_collage_probatory_document.Any(entity => entity.probatory_document_id == item2.ProbatoryDocumentId && entity.sort_priority == item2.SortPriority && entity.insurance_collage_id == item2.InsuranceCollageId));
			// Verify that items are updated in the cache
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments, It.IsAny<List<InsuranceCollageProbatoryDocument>>()), Times.Once);
			// Verify the change on sort priority is being added to cache
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments, It.Is<List<InsuranceCollageProbatoryDocument>>(cache => cache.Any(item => item.ProbatoryDocumentId == item1.ProbatoryDocumentId && item.SortPriority == item1.SortPriority && item.InsuranceCollageId == item1.InsuranceCollageId))), Times.Once);
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments, It.Is<List<InsuranceCollageProbatoryDocument>>(cache => cache.Any(item => item.ProbatoryDocumentId == item2.ProbatoryDocumentId && item.SortPriority == item2.SortPriority && item.InsuranceCollageId == item2.InsuranceCollageId))), Times.Once);

			#endregion
		}

		[Test]
		public async Task UpsertInsuranceCollageProbatoryDocuments_WhenInsertDocuments_ThenApplyChangesToDatabase()
		{
			#region Arrange

			// These are the mocked records from cache
			object cachedInsuranceCollageProbatoryDocuments = new List<InsuranceCollageProbatoryDocument>
			{
				// Collage #1: Contains 2 probatory documents and 1 more will be added
				new InsuranceCollageProbatoryDocument
				{
					Id = 1,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 1,
					SortPriority = 1
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 2,
					InsuranceCollageId = 1,
					ProbatoryDocumentId = 2,
					SortPriority = 2
				},
				// Collage #2: Contains 2 probatory documents
				new InsuranceCollageProbatoryDocument
				{
					Id = 4,
					InsuranceCollageId = 2,
					ProbatoryDocumentId = 1,
					SortPriority = 1
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 5,
					InsuranceCollageId = 2,
					ProbatoryDocumentId = 4,
					SortPriority = 2
				}
			};

			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceCollageProbatoryDocuments))).Returns(true);
			_cacheService.Setup(x => x.Get(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments)).Returns(cachedInsuranceCollageProbatoryDocuments);

			// These are the mocked records from db
			var insurance_collage_probatory_document_entities = new List<insurance_collage_probatory_document>
			{
				// Collage #1: Contains 2 probatory documents and 1 more will be added
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 1,
					insurance_collage_id = 1,
					probatory_document_id = 1,
					sort_priority = 1
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 2,
					insurance_collage_id = 1,
					probatory_document_id = 2,
					sort_priority = 2
				},
				// Collage #2: Contains 2 probatory documents
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 4,
					insurance_collage_id = 2,
					probatory_document_id = 1,
					sort_priority = 1
				},
				new insurance_collage_probatory_document
				{
					insurance_collage_probatory_document_id = 5,
					insurance_collage_id = 2,
					probatory_document_id = 4,
					sort_priority = 2
				}
			};

			// Initialize DbSet in context
			_context.Setup(x => x.insurance_collage_probatory_document).ReturnsDbSet(insurance_collage_probatory_document_entities);
			this.SetupContext();

			// The items #3 in the list below is being added to collage #1
			// IMPORTANT: The sortpriority is initialized in the back end and it will depend in the position they have in the request
			var collageId = 1;
			var newCollageDocuments = new List<InsuranceCollageProbatoryDocument>
			{
				new InsuranceCollageProbatoryDocument
				{
					Id = 1,
					InsuranceCollageId = collageId,
					ProbatoryDocumentId = 1,
					//SortPriority = 1 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				},
				new InsuranceCollageProbatoryDocument
				{
					Id = 2,
					InsuranceCollageId = collageId,
					ProbatoryDocumentId = 2,
					//SortPriority = 2 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				},
				// The item below is being added to collage #1
				new InsuranceCollageProbatoryDocument
				{
					Id = 3,
					InsuranceCollageId = collageId,
					ProbatoryDocumentId = 3,
					//SortPriority = 3 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				}
			};

			#endregion

			#region Act

			await service.UpsertInsuranceCollageProbatoryDocuments(newCollageDocuments, collageId).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify there aren't any records being deleted from db
			_context.Verify(v => v.insurance_collage_probatory_document.Remove(It.IsAny<insurance_collage_probatory_document>()), Times.Never);
			// Verify that new items are added to db
			_context.Verify(v => v.insurance_collage_probatory_document.Add(It.IsAny<insurance_collage_probatory_document>()), Times.AtLeastOnce);
			// Verify that new items are added to cache
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments, It.IsAny<List<InsuranceCollageProbatoryDocument>>()), Times.Once);
			// Verify the number of items added to cache is correct
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceCollageProbatoryDocuments, It.Is<List<InsuranceCollageProbatoryDocument>>(x => x.Where(w => w.InsuranceCollageId == newCollageDocuments.First().InsuranceCollageId).Count() == newCollageDocuments.Count())), Times.Once);

			#endregion
		}

		#endregion
	}
}
