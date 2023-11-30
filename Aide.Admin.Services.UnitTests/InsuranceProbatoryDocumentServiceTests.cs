using Aide.Core.Interfaces;
using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Moq;
using Moq.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aide.Admin.Services.UnitTests
{
    [TestFixture]
    public class InsuranceProbatoryDocumentServiceTests
    {
        private Mock<IServiceProvider> _serviceProvider;
        private Mock<ICacheService> _cacheService;
        private Mock<IProbatoryDocumentService> _probatoryDocumentService;
        private Mock<AideDbContext> _context;
        private const string _cacheKeyNameForDtoInsuranceProbatoryDocuments = "Dto-List-InsuranceProbatoryDocument";
        private IInsuranceProbatoryDocumentService service;

        [SetUp]
        public void Setup()
        {
            _serviceProvider = new Mock<IServiceProvider>();
            _cacheService = new Mock<ICacheService>();
            _probatoryDocumentService = new Mock<IProbatoryDocumentService>();
            _context = new Mock<AideDbContext>();
            service = new InsuranceProbatoryDocumentService(_serviceProvider.Object, _cacheService.Object, _probatoryDocumentService.Object);
        }

        /// <summary>
        /// Setup the context.
        /// Need call this in scenarios that will query the database.
        /// IMPORTANT: The entity initialization must happen before calling this.
        /// </summary>
        private void SetupContext()
        {
            // For passing the DbSet from context to EfRepository
            _context.Setup(x => x.Set<insurance_probatory_document>()).Returns(_context.Object.insurance_probatory_document);
            // Initialize context scope
            _serviceProvider.Setup(x => x.GetService(typeof(AideDbContext))).Returns(_context.Object);
            // For initialization of context in EfRepository (it seems like this step is not needed)
            //_serviceProvider.Setup(x => x.GetService(typeof(DbContext))).Returns(_context.Object);
        }

        #region GetAllInsuranceProbatoryDocuments

        [Test]
        public async Task GetAllInsuranceProbatoryDocuments_WhenIsFirstTimeCall_ThenPullDataFromDbAndCacheIt()
        {
            #region Arrange

            // These are the mocked records from db
            var insurance_probatory_document_entities = new List<insurance_probatory_document>
            {
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 1,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 1,
                    group_id = 1,
                    sort_priority = 1
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 2,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 2,
                    group_id = 2,
                    sort_priority = 2
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 99,
                    insurance_company_id = 99,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 99,
                    group_id = 99,
                    sort_priority = 99
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 100,
                    insurance_company_id = 100,
                    claim_type_id = (int)EnumClaimTypeId.Colision,
                    probatory_document_id = 100,
                    group_id = 100,
                    sort_priority = 100
                }
            };
            // Metadata for insurance probatory documents
            var CachedProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };

            // When method is called by first time there's nothing in cache
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(false);
            // Initialize DbSet in context
            _context.Setup(x => x.insurance_probatory_document).ReturnsDbSet(insurance_probatory_document_entities);
            this.SetupContext();
            // Load metadata for insurance probatory documents (this one is already cached)
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(CachedProbatoryDocumentMetadata);

            #endregion

            #region Act

            var result = await service.GetAllInsuranceProbatoryDocuments().ConfigureAwait(false);

            #endregion

            #region Assert

            // Look for a cache entry
            _cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.AtLeastOnce);
            // Since there's no cache then it won't try pull data from there
            _cacheService.Verify(v => v.Get(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Never);
            // Initialize a db context
            _serviceProvider.Verify(v => v.GetService(typeof(AideDbContext)), Times.Once);
            // Pull data from db one time only
            _context.Verify(v => v.Set<insurance_probatory_document>(), Times.Once);
            // ... and cache the data
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.IsAny<List<InsuranceProbatoryDocument>>()), Times.Once);
            // Finally, it loads metadata (this comes from another service)
            _probatoryDocumentService.Verify(v => v.GetProbatoryDocumentListByIds(It.IsAny<int[]>()), Times.Once);
            // Verify the result is not null
            Assert.IsNotNull(result);
            // Verify the result has the same quantity of items as the number of rows in db
            Assert.AreEqual(result.Count(), insurance_probatory_document_entities.Count());
            // Verify that all items have metadata
            Assert.IsTrue(!result.Any(x => x.ProbatoryDocument == null));

            #endregion
        }

        [Test]
        public async Task GetAllInsuranceProbatoryDocuments_WhenIsSecondTimeCallAndGoingForward_ThenPullDataFromCache()
        {
            #region Arrange

            // These are cached records (notice that it is an object type)
            IEnumerable<InsuranceProbatoryDocument> cachedInsuranceProbatoryDocuments = new List<InsuranceProbatoryDocument>
            {
                new InsuranceProbatoryDocument
                {
                    Id = 1,
                    InsuranceCompanyId = 1,
                    ProbatoryDocumentId = 1
                },
                new InsuranceProbatoryDocument
                {
                    Id = 2,
                    InsuranceCompanyId = 1,
                    ProbatoryDocumentId = 2
                },
                new InsuranceProbatoryDocument
                {
                    Id = 99,
                    InsuranceCompanyId = 99,
                    ProbatoryDocumentId = 99
                },
                new InsuranceProbatoryDocument
                {
                    Id = 100,
                    InsuranceCompanyId = 100,
                    ProbatoryDocumentId = 100
                }
            };
            // Metadata for insurance probatory documents
            var ProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };

            // When method is called by second time and going forward data is pulled from cache only
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(true);
            _cacheService.Setup(x => x.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(cachedInsuranceProbatoryDocuments);
            // Load metadata for insurance probatory documents
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(ProbatoryDocumentMetadata);

            #endregion

            #region Act

            var result = await service.GetAllInsuranceProbatoryDocuments().ConfigureAwait(false);

            #endregion

            #region Assert

            // Look for a cache entry
            _cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Once);
            // It pulls data from cache always
            _cacheService.Verify(v => v.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Once);
            // It NEVER initialize a db context scope
            _serviceProvider.Verify(v => v.GetService(typeof(AideDbContext)), Times.Never);
            // so that, it NEVER pulls data from db
            _context.Verify(v => v.Set<insurance_probatory_document>(), Times.Never);
            // Finally, it loads metadata (this comes from another service)
            _probatoryDocumentService.Verify(v => v.GetProbatoryDocumentListByIds(It.IsAny<int[]>()), Times.Once);
            // Verify the result is not null
            Assert.IsNotNull(result);
            // Verify the result has the same quantity of items as the number of rows in db
            Assert.AreEqual(result.Count(), cachedInsuranceProbatoryDocuments.Count());
            // Verify that all items have metadata
            Assert.IsTrue(!result.Any(x => x.ProbatoryDocument == null));

            #endregion
        }

        #endregion

        #region GetInsuranceProbatoryDocumentById

        [Test]
        public async Task GetInsuranceProbatoryDocumentById_WhenIsFirstTimeCall_ThenPullDataFromDbAndLoadMetadata()
        {
            #region Arrange

            // Required argument
            var insuranceProbatoryDocumentId = 1;
            // These are the mocked records from db
            var insurance_probatory_document_entities = new List<insurance_probatory_document>
            {
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 1,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 1,
                    group_id = 1,
                    sort_priority = 1
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 2,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 2,
                    group_id = 2,
                    sort_priority = 2
                }
            };
            // Metadata for insurance probatory documents
            var CachedProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                }
            };

            // When method is called by first time there's nothing in cache
            _cacheService.Setup(x => x.Exist(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(false);
            // Initialize DbSet in context
            _context.Setup(x => x.insurance_probatory_document).ReturnsDbSet(insurance_probatory_document_entities);
            this.SetupContext();
            // Load metadata for insurance probatory documents (this one is already cached)
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(CachedProbatoryDocumentMetadata);

            #endregion

            #region Act

            var result = await service.GetInsuranceProbatoryDocumentById(insuranceProbatoryDocumentId).ConfigureAwait(false);

            #endregion

            #region Assert

            // Verify if exist a cache entry
            _cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Once);
            // Since there's no cache entry then it won't try to pull any data from cache
            _cacheService.Verify(v => v.Get(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Never);
            // Initialize a db context
            _serviceProvider.Verify(v => v.GetService(typeof(AideDbContext)), Times.Once);
            // Pull data from db one time only
            _context.Verify(v => v.Set<insurance_probatory_document>(), Times.Once);
            // ... and cache the data
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.IsAny<List<InsuranceProbatoryDocument>>()), Times.Once);
            // Finally, it loads metadata (this comes from another service)
            _probatoryDocumentService.Verify(v => v.GetProbatoryDocumentListByIds(It.IsAny<int[]>()), Times.Once);
            // Verify the result is not null
            Assert.IsNotNull(result);
            // Verify the insurance probatory document ID is the one expected
            Assert.IsTrue(result.Id == insuranceProbatoryDocumentId);
            // Verify the metadata is not null
            Assert.IsNotNull(result.ProbatoryDocument);

            #endregion
        }

        [Test]
        public async Task GetInsuranceProbatoryDocumentById_WhenIsSecondTimeCallAndGoingForward_ThenPullDataFromCacheAndLoadMetadata()
        {
            #region Arrange

            // Required argument
            var insuranceProbatoryDocumentId = 1;
            // These are cached records (notice that it is an object type)
            IEnumerable<InsuranceProbatoryDocument> cachedInsuranceProbatoryDocuments = new List<InsuranceProbatoryDocument>
            {
                new InsuranceProbatoryDocument
                {
                    Id = 1,
                    ProbatoryDocumentId = 1,
                    ProbatoryDocument = null // This will initialize from different service
				},
                new InsuranceProbatoryDocument
                {
                    Id = 2,
                    ProbatoryDocumentId = 2,
                    ProbatoryDocument = null // This will initialize from different service
				}
            };
            // Metadata for insurance probatory documents
            var ProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                }
            };

            // When method is called by second time and going forward data is pulled from cache only
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(true);
            _cacheService.Setup(x => x.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(cachedInsuranceProbatoryDocuments);
            // Load metadata for insurance probatory documents
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(ProbatoryDocumentMetadata);

            #endregion

            #region Act

            var result = await service.GetInsuranceProbatoryDocumentById(insuranceProbatoryDocumentId).ConfigureAwait(false);

            #endregion

            #region Assert

            // Look for a cache entry
            _cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Once);
            // It pulls data from cache always
            _cacheService.Verify(v => v.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Once);
            // It NEVER initialize a db context scope
            _serviceProvider.Verify(v => v.GetService(typeof(AideDbContext)), Times.Never);
            // so that, it NEVER pulls data from db
            _context.Verify(v => v.Set<insurance_probatory_document>(), Times.Never);
            // Finally, it loads metadata (this comes from another service)
            _probatoryDocumentService.Verify(v => v.GetProbatoryDocumentListByIds(It.IsAny<int[]>()), Times.Once);
            // Verify the result is not null
            Assert.IsNotNull(result);
            // Verify the insurance probatory document ID is the one expected
            Assert.IsTrue(result.Id == insuranceProbatoryDocumentId);
            // Verify the metadata is not null
            Assert.IsNotNull(result.ProbatoryDocument);

            #endregion
        }

        #endregion

        #region GetInsuranceProbatoryDocumentsByInsuranceId

        [Test]
        public async Task GetInsuranceProbatoryDocumentsByInsuranceId_WhenIsFirstTimeCall_ThenPullDataFromDbAndLoadMetadata()
        {
            #region Arrange

            // Required argument
            var insuranceId = 1;
            // These are the mocked records from db
            var insurance_probatory_document_entities = new List<insurance_probatory_document>
            {
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 1,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 1,
                    group_id = 1,
                    sort_priority = 1
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 2,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 2,
                    group_id = 2,
                    sort_priority = 2
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 99,
                    insurance_company_id = 99,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 99,
                    group_id = 99,
                    sort_priority = 99
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 100,
                    insurance_company_id = 100,
                    claim_type_id = (int)EnumClaimTypeId.Colision,
                    probatory_document_id = 100,
                    group_id = 100,
                    sort_priority = 100
                }
            };
            // Metadata for insurance probatory documents
            var CachedProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };

            // When method is called by first time there's nothing in cache
            _cacheService.Setup(x => x.Exist(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(false);
            // Initialize DbSet in context
            _context.Setup(x => x.insurance_probatory_document).ReturnsDbSet(insurance_probatory_document_entities);
            this.SetupContext();
            // Load metadata for insurance probatory documents (this one is already cached)
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(CachedProbatoryDocumentMetadata);

            #endregion

            #region Act

            var result = await service.GetInsuranceProbatoryDocumentsByInsuranceId(insuranceId).ConfigureAwait(false);

            #endregion

            #region Assert

            // Verify if exist a cache entry
            _cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Once);
            // Since there's no cache entry then it won't try to pull any data from cache
            _cacheService.Verify(v => v.Get(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Never);
            // Initialize a db context
            _serviceProvider.Verify(v => v.GetService(typeof(AideDbContext)), Times.Once);
            // Pull data from db one time only
            _context.Verify(v => v.Set<insurance_probatory_document>(), Times.Once);
            // ... and cache the data
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.IsAny<List<InsuranceProbatoryDocument>>()), Times.Once);
            // Finally, it loads metadata (this comes from another service)
            _probatoryDocumentService.Verify(v => v.GetProbatoryDocumentListByIds(It.IsAny<int[]>()), Times.Once);
            // Verify the result is not null
            Assert.IsNotNull(result);
            // Verify the result has the correct number of items
            Assert.AreEqual(result.Count(), insurance_probatory_document_entities.Where(x => x.insurance_company_id == insuranceId).Count());
            // Verify all items has the correct insurance ID
            Assert.IsTrue(!result.Any(x => x.InsuranceCompanyId != insuranceId));
            // Verify the metadata on items is not null
            Assert.IsTrue(!result.Any(x => x.ProbatoryDocument == null));

            #endregion
        }

        [Test]
        public async Task GetInsuranceProbatoryDocumentsByInsuranceId_WhenIsSecondTimeCallAndGoingForward_ThenPullDataFromCacheAndLoadMetadata()
        {
            #region Arrange

            // Required argument
            var insuranceId = 1;
            // These are cached records (notice that it is an object type)
            IEnumerable<InsuranceProbatoryDocument> cachedInsuranceProbatoryDocuments = new List<InsuranceProbatoryDocument>
            {
                new InsuranceProbatoryDocument
                {
                    Id = 1,
                    InsuranceCompanyId = 1,
                    ProbatoryDocumentId = 1
                },
                new InsuranceProbatoryDocument
                {
                    Id = 2,
                    InsuranceCompanyId = 1,
                    ProbatoryDocumentId = 2
                },
                new InsuranceProbatoryDocument
                {
                    Id = 99,
                    InsuranceCompanyId = 99,
                    ProbatoryDocumentId = 99
                },
                new InsuranceProbatoryDocument
                {
                    Id = 100,
                    InsuranceCompanyId = 100,
                    ProbatoryDocumentId = 100
                }
            };
            // Metadata for insurance probatory documents
            var ProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };

            // When method is called by second time and going forward data is pulled from cache only
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(true);
            _cacheService.Setup(x => x.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(cachedInsuranceProbatoryDocuments);
            // Load metadata for insurance probatory documents
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(ProbatoryDocumentMetadata);

            #endregion

            #region Act

            var result = await service.GetInsuranceProbatoryDocumentsByInsuranceId(insuranceId).ConfigureAwait(false);

            #endregion

            #region Assert

            // Look for a cache entry
            _cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Once);
            // It pulls data from cache always
            _cacheService.Verify(v => v.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments), Times.Once);
            // It NEVER initialize a db context scope
            _serviceProvider.Verify(v => v.GetService(typeof(AideDbContext)), Times.Never);
            // so that, it NEVER pulls data from db
            _context.Verify(v => v.Set<insurance_probatory_document>(), Times.Never);
            // Finally, it loads metadata (this comes from another service)
            _probatoryDocumentService.Verify(v => v.GetProbatoryDocumentListByIds(It.IsAny<int[]>()), Times.Once);
            // Verify the result is not null
            Assert.IsNotNull(result);
            // Verify the result has the correct number of items
            Assert.AreEqual(result.Count(), cachedInsuranceProbatoryDocuments.Where(x => x.InsuranceCompanyId == insuranceId).Count());
            // Verify all items has the correct insurance ID
            Assert.IsTrue(!result.Any(x => x.InsuranceCompanyId != insuranceId));
            // Verify the metadata on items is not null
            Assert.IsTrue(!result.Any(x => x.ProbatoryDocument == null));

            #endregion
        }

        #endregion

        #region GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId

        [Test]
        public async Task GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId_WhenExist_ThenReturnInsuranceProbatoryDocuments()
        {
            #region Arrange

            // Required inputs
            var insuranceCompanyId = 1;
            var claimTypeId = EnumClaimTypeId.Siniestro;
            // Assuming the records are in cache
            IEnumerable<InsuranceProbatoryDocument> cachedInsuranceProbatoryDocuments = new List<InsuranceProbatoryDocument>
            {
                new InsuranceProbatoryDocument
                {
                    Id = 1,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
                    SortPriority = 1
                },
                new InsuranceProbatoryDocument
                {
                    Id = 2,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 2,
                    GroupId = 2,
                    SortPriority = 2
                },
                new InsuranceProbatoryDocument
                {
                    Id = 99,
                    InsuranceCompanyId = 99,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 99,
                    GroupId = 99,
                    SortPriority = 99
                },
                new InsuranceProbatoryDocument
                {
                    Id = 100,
                    InsuranceCompanyId = 100,
                    ClaimTypeId = EnumClaimTypeId.Colision,
                    ProbatoryDocumentId = 100,
                    GroupId = 100,
                    SortPriority = 100
                }
            };
            // Metadata for insurance probatory documents which is in cache
            var CachedProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };

            // The cache entry exist
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(true);
            _cacheService.Setup(x => x.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(cachedInsuranceProbatoryDocuments);
            // Load metadata for insurance probatory documents
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(CachedProbatoryDocumentMetadata);

            #endregion

            #region Act

            var result = await service.GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(insuranceCompanyId, claimTypeId).ConfigureAwait(false);

            #endregion

            #region Assert

            // The result shouldn't be NULL
            Assert.IsNotNull(result);
            // The result is not empty
            Assert.IsTrue(result.Any());
            // The number of items match the count of cached items that are related to the insurance company and claim type provided
            Assert.AreEqual(result.Count(x => x.InsuranceCompanyId == insuranceCompanyId && x.ClaimTypeId == claimTypeId), cachedInsuranceProbatoryDocuments.Count(cache => cache.InsuranceCompanyId == insuranceCompanyId && cache.ClaimTypeId == claimTypeId));

            #endregion
        }

        [Test]
        public async Task GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId_WhenDoNotExist_ThenReturnEmptyResult()
        {
            #region Arrange

            // Required inputs
            var insuranceCompanyId = 101; // This doesn't exist
            var claimTypeId = EnumClaimTypeId.Siniestro;
            // Assuming the records are in cache
            object cachedInsuranceProbatoryDocuments = new List<InsuranceProbatoryDocument>
            {
                new InsuranceProbatoryDocument
                {
                    Id = 1,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
                    SortPriority = 1
                },
                new InsuranceProbatoryDocument
                {
                    Id = 2,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 2,
                    GroupId = 2,
                    SortPriority = 2
                },
                new InsuranceProbatoryDocument
                {
                    Id = 99,
                    InsuranceCompanyId = 99,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 99,
                    GroupId = 99,
                    SortPriority = 99
                },
                new InsuranceProbatoryDocument
                {
                    Id = 100,
                    InsuranceCompanyId = 100,
                    ClaimTypeId = EnumClaimTypeId.Colision,
                    ProbatoryDocumentId = 100,
                    GroupId = 100,
                    SortPriority = 100
                }
            };
            // Metadata for insurance probatory documents which is in cache
            var CachedProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };

            // The cache entry exist
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(true);
            _cacheService.Setup(x => x.Get(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(cachedInsuranceProbatoryDocuments);
            // Load metadata for insurance probatory documents
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(CachedProbatoryDocumentMetadata);

            #endregion

            #region Act

            var result = await service.GetInsuranceProbatoryDocumentsByInsuranceIdAndClaimTypeId(insuranceCompanyId, claimTypeId).ConfigureAwait(false);

            #endregion

            #region Assert

            // The result shouldn't be NULL
            Assert.IsNotNull(result);
            // The result is empty
            Assert.IsFalse(result.Any());

            #endregion
        }

        #endregion

        #region UpsertInsuranceProbatoryDocuments

        #region Insert

        [Test]
        public async Task UpsertInsuranceProbatoryDocuments_WhenInsertedInsuranceProbatoryDocument_ThenInsertInBothDbAndCache()
        {
            #region Arrange

            // Required inputs
            var insuranceCompanyId = 1;
            var claimTypeId = EnumClaimTypeId.Siniestro;
            // In this scenario I'm pretending there are no documents currently associated to this insurance company
            // so that the items below will be inserted
            var upsertRequest = new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest[]
            {
                new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest
                {
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
					//SortPriority = 0 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				},
                new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest
                {
                    ProbatoryDocumentId = 2,
                    GroupId = 2,
					//SortPriority = 0 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				}
            };

            // For this scenario I'm pretending there's nothing in cache
            object cachedInsuranceProbatoryDocuments = new List<InsuranceProbatoryDocument>();
            // Metadata for insurance probatory documents (for this scenario I'm pretending the metadata exist in cache already)
            var CachedProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };
            // For this scenario I'm pretending there's nothing in database
            var insurance_probatory_document_entities = new List<insurance_probatory_document>();

            // To make things simple I'm pretending the cache entry exist already
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(true);
            _cacheService.Setup(x => x.Get(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(cachedInsuranceProbatoryDocuments);
            // Load metadata for insurance probatory documents
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(CachedProbatoryDocumentMetadata);

            // Initialize DbSet in context
            _context.Setup(x => x.insurance_probatory_document).ReturnsDbSet(insurance_probatory_document_entities);
            this.SetupContext();

            #endregion

            #region Act

            await service.UpsertInsuranceProbatoryDocuments(insuranceCompanyId, claimTypeId, upsertRequest).ConfigureAwait(false);

            #endregion

            #region Assert

            // Verify there aren't any records being deleted from db
            _context.Verify(v => v.insurance_probatory_document.Remove(It.IsAny<insurance_probatory_document>()), Times.Never);
            // Verify that new items are added to db
            _context.Verify(v => v.insurance_probatory_document.Add(It.IsAny<insurance_probatory_document>()), Times.AtLeastOnce);
            // Verify that new items are added to cache
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.IsAny<List<InsuranceProbatoryDocument>>()), Times.Once);
            // Verify the number of items added to cache is corrrect
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.Is<List<InsuranceProbatoryDocument>>(x => x.Count() == upsertRequest.Count())), Times.Once);

            #endregion
        }

        [Test]
        public async Task UpsertInsuranceProbatoryDocuments_WhenInsertedInsuranceProbatoryDocumentAndTheyArePreexistingItemsInTheSameInsuranceCompany_ThenInsertInBothDbAndCache()
        {
            #region Arrange

            // Required inputs
            var insuranceCompanyId = 1;
            var claimTypeId = EnumClaimTypeId.Siniestro;
            // In this scenario only the first doc in the request it's previously associated to this insurance company
            // so that only the second item will be inserted/appended
            var upsertRequest = new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest[]
            {
                new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest
                {
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
					//SortPriority = 0 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				},
                new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest
                {
                    ProbatoryDocumentId = 2,
                    GroupId = 2,
					//SortPriority = 0 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				}
            };

            // Only the first doc in the request exist in cache
            IEnumerable<InsuranceProbatoryDocument> cachedInsuranceProbatoryDocuments = new List<InsuranceProbatoryDocument>
            {
                new InsuranceProbatoryDocument
                {
                    Id = 1,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
                    SortPriority = 1
                }
            };
            // Metadata for insurance probatory documents (for this scenario I'm pretending the metadata exist in cache already)
            var CachedProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };
            // Only the fist doc in the request exist in database
            var insurance_probatory_document_entities = new List<insurance_probatory_document>
            {
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 1,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 1,
                    group_id = 1,
                    sort_priority = 1
                }
            };

            // The cache entry exist already
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(true);
            _cacheService.Setup(x => x.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(cachedInsuranceProbatoryDocuments);
            // Load metadata for insurance probatory documents
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(CachedProbatoryDocumentMetadata);

            // Initialize DbSet in context
            _context.Setup(x => x.insurance_probatory_document).ReturnsDbSet(insurance_probatory_document_entities);
            this.SetupContext();

            #endregion

            #region Act

            await service.UpsertInsuranceProbatoryDocuments(insuranceCompanyId, claimTypeId, upsertRequest).ConfigureAwait(false);

            #endregion

            #region Assert

            // Verify there aren't any records being deleted from db
            _context.Verify(v => v.insurance_probatory_document.Remove(It.IsAny<insurance_probatory_document>()), Times.Never);
            // Verify that new items are added to db (here it's once because there's one single doc being added)
            _context.Verify(v => v.insurance_probatory_document.Add(It.IsAny<insurance_probatory_document>()), Times.Once);
            // Verify that new items are added to cache
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.IsAny<List<InsuranceProbatoryDocument>>()), Times.Once);
            // Verify the number of items added to cache is correct (formula: original items in db + new item(s) in the request)
            var newItemsCount = insurance_probatory_document_entities.Count() + upsertRequest.Count(request => !insurance_probatory_document_entities.Any(db_row => db_row.probatory_document_id == request.ProbatoryDocumentId));
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.Is<List<InsuranceProbatoryDocument>>(x => x.Count() == newItemsCount)), Times.Once);

            #endregion
        }

        #endregion

        #region Update

        [Test]
        public async Task UpsertInsuranceProbatoryDocuments_WhenUpdatedInsuranceProbatoryDocumentGroupId_ThenUpdateInBothDbAndCache()
        {
            #region Arrange

            // Required inputs
            var insuranceCompanyId = 1;
            var claimTypeId = EnumClaimTypeId.Siniestro;
            // The group id on the second item is being changed from 2 to 1
            var upsertRequest = new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest[]
            {
                new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest
                {
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
					//SortPriority = 0 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				},
                new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest
                {
                    ProbatoryDocumentId = 2,
                    GroupId = 1,
					//SortPriority = 0 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				}
            };

            // Both items exist in cache
            object cachedInsuranceProbatoryDocuments = new List<InsuranceProbatoryDocument>
            {
                new InsuranceProbatoryDocument
                {
                    Id = 1,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
                    SortPriority = 1
                },
                new InsuranceProbatoryDocument
                {
                    Id = 2,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 2,
                    GroupId = 2, // Notice that in cache the current group id is 2
					SortPriority = 2
                }
            };
            // Metadata for insurance probatory documents (for this scenario I'm pretending the metadata exist in cache already)
            var CachedProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };
            // Both items exist in database
            var insurance_probatory_document_entities = new List<insurance_probatory_document>
            {
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 1,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 1,
                    group_id = 1,
                    sort_priority = 1
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 2,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 2,
                    group_id = 2, // Notice that in database the current group id is 2
					sort_priority = 2
                }
            };

            // The cache entry exist already
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(true);
            _cacheService.Setup(x => x.Get(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(cachedInsuranceProbatoryDocuments);
            // Load metadata for insurance probatory documents
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(CachedProbatoryDocumentMetadata);

            // Initialize DbSet in context
            _context.Setup(x => x.insurance_probatory_document).ReturnsDbSet(insurance_probatory_document_entities);
            this.SetupContext();

            #endregion

            #region Act

            await service.UpsertInsuranceProbatoryDocuments(insuranceCompanyId, claimTypeId, upsertRequest).ConfigureAwait(false);

            #endregion

            #region Assert

            // Verify there aren't any records being deleted from db
            _context.Verify(v => v.insurance_probatory_document.Remove(It.IsAny<insurance_probatory_document>()), Times.Never);
            // Verify that items are updated in the database
            Assert.IsTrue(_context.Object.insurance_probatory_document.Any(entity => entity.probatory_document_id == upsertRequest.First().ProbatoryDocumentId && entity.group_id == upsertRequest.First().GroupId));
            // Verify that items are updated in the cache
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.IsAny<List<InsuranceProbatoryDocument>>()), Times.Once);
            // Verify the change on group id is being added to cache
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.Is<List<InsuranceProbatoryDocument>>(cache => cache.Any(item => item.ProbatoryDocumentId == upsertRequest.First().ProbatoryDocumentId && item.GroupId == upsertRequest.First().GroupId))), Times.Once);

            #endregion
        }

        [Test]
        public async Task UpsertInsuranceProbatoryDocuments_WhenUpdatedInsuranceProbatoryDocumentSortPriority_ThenUpdateInBothDbAndCache()
        {
            #region Arrange

            // Required inputs
            var insuranceCompanyId = 1;
            var claimTypeId = EnumClaimTypeId.Siniestro;
            // The items have been inverted. The sortpriority is determined in the back end and will depend in the position they have in the request
            var upsertRequest = new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest[]
            {
                new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest
                {
                    ProbatoryDocumentId = 2,
                    GroupId = 2,
					//SortPriority = 0 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				},
                new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest
                {
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
					//SortPriority = 0 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				}
            };

            // Both items exist in cache
            IEnumerable<InsuranceProbatoryDocument> cachedInsuranceProbatoryDocuments = new List<InsuranceProbatoryDocument>
            {
                new InsuranceProbatoryDocument
                {
                    Id = 1,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
                    SortPriority = 1 // Sort prioriy in cache
				},
                new InsuranceProbatoryDocument
                {
                    Id = 2,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 2,
                    GroupId = 2,
                    SortPriority = 2 // Sort prioriy in cache
				}
            };
            // Metadata for insurance probatory documents (for this scenario I'm pretending the metadata exist in cache already)
            var CachedProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };
            // Both items exist in database
            var insurance_probatory_document_entities = new List<insurance_probatory_document>
            {
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 1,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 1,
                    group_id = 1,
                    sort_priority = 1 // Sort prioriy in database
				},
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 2,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 2,
                    group_id = 2,
                    sort_priority = 2 // Sort prioriy in database
				}
            };

            // The cache entry exist already
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(true);
            _cacheService.Setup(x => x.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(cachedInsuranceProbatoryDocuments);
            // Load metadata for insurance probatory documents
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(CachedProbatoryDocumentMetadata);

            // Initialize DbSet in context
            _context.Setup(x => x.insurance_probatory_document).ReturnsDbSet(insurance_probatory_document_entities);
            this.SetupContext();

            #endregion

            #region Act

            await service.UpsertInsuranceProbatoryDocuments(insuranceCompanyId, claimTypeId, upsertRequest).ConfigureAwait(false);

            #endregion

            #region Assert

            // Verify there aren't any records being deleted from db
            _context.Verify(v => v.insurance_probatory_document.Remove(It.IsAny<insurance_probatory_document>()), Times.Never);
            // Verify that items are updated in the database
            var item1 = upsertRequest.First();
            var item2 = upsertRequest[1];
            Assert.IsTrue(_context.Object.insurance_probatory_document.Any(entity => entity.probatory_document_id == item1.ProbatoryDocumentId && entity.sort_priority == item1.SortPriority));
            Assert.IsTrue(_context.Object.insurance_probatory_document.Any(entity => entity.probatory_document_id == item2.ProbatoryDocumentId && entity.sort_priority == item2.SortPriority));
            // Verify that items are updated in the cache
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.IsAny<List<InsuranceProbatoryDocument>>()), Times.Once);
            // Verify the change on sort priority is being added to cache
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.Is<List<InsuranceProbatoryDocument>>(cache => cache.Any(item => item.ProbatoryDocumentId == item1.ProbatoryDocumentId && item.SortPriority == item1.SortPriority))), Times.Once);
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, It.Is<List<InsuranceProbatoryDocument>>(cache => cache.Any(item => item.ProbatoryDocumentId == item2.ProbatoryDocumentId && item.SortPriority == item2.SortPriority))), Times.Once);

            #endregion
        }

        #endregion

        #region Delete

        [Test]
        public async Task UpsertInsuranceProbatoryDocuments_WhenDeletedInsuranceProbatoryDocument_ThenDeleteFromBothDbAndCache()
        {
            #region Arrange

            // Required inputs
            var insuranceCompanyId = 1;
            var claimTypeId = EnumClaimTypeId.Siniestro;
            // Here you have the probatory documents that you want to keep, any missing document here will be deleted from the pre-existing list
            var upsertRequest = new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest[]
            {
                new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentUpsertRequest
                {
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
                    SortPriority = 0 // IMPORTANT: This value is not coming from the front-end but is initialized in the back-end
				}
            };
            // These are the cached insurance probatory document that includes all probatory documents for all insurance companies for all types of service (notice that it is an object type)
            IEnumerable<InsuranceProbatoryDocument> cachedInsuranceProbatoryDocuments = new List<InsuranceProbatoryDocument>
            {
                new InsuranceProbatoryDocument
                {
                    Id = 1,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 1,
                    GroupId = 1,
                    SortPriority = 1
                },
                new InsuranceProbatoryDocument
                {
                    Id = 2,
                    InsuranceCompanyId = 1,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 2,
                    GroupId = 2,
                    SortPriority = 2
                },
                new InsuranceProbatoryDocument
                {
                    Id = 99,
                    InsuranceCompanyId = 99,
                    ClaimTypeId = EnumClaimTypeId.Siniestro,
                    ProbatoryDocumentId = 99,
                    GroupId = 99,
                    SortPriority = 99
                },
                new InsuranceProbatoryDocument
                {
                    Id = 100,
                    InsuranceCompanyId = 100,
                    ClaimTypeId = EnumClaimTypeId.Colision,
                    ProbatoryDocumentId = 100,
                    GroupId = 100,
                    SortPriority = 100
                }
            };
            // Metadata for insurance probatory documents
            var CachedProbatoryDocumentMetadata = new List<ProbatoryDocument>
            {
                new ProbatoryDocument
                {
                    Id = 1,
                    Name = "Probatory Document 1"
                },
                new ProbatoryDocument
                {
                    Id = 2,
                    Name = "Probatory Document 2"
                },
                new ProbatoryDocument
                {
                    Id = 99,
                    Name = "Probatory Document 99"
                },
                new ProbatoryDocument
                {
                    Id = 100,
                    Name = "Probatory Document 100"
                }
            };

            // These are the mocked records from db
            var insurance_probatory_document_entities = new List<insurance_probatory_document>
            {
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 1,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 1,
                    group_id = 1,
                    sort_priority = 1
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 2,
                    insurance_company_id = 1,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 2,
                    group_id = 2,
                    sort_priority = 2
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 99,
                    insurance_company_id = 99,
                    claim_type_id = (int)EnumClaimTypeId.Siniestro,
                    probatory_document_id = 99,
                    group_id = 99,
                    sort_priority = 99
                },
                new insurance_probatory_document
                {
                    insurance_probatory_document_id = 100,
                    insurance_company_id = 100,
                    claim_type_id = (int)EnumClaimTypeId.Colision,
                    probatory_document_id = 100,
                    group_id = 100,
                    sort_priority = 100
                }
            };

            // To make things simple the data will be pulled from cache
            _cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoInsuranceProbatoryDocuments))).Returns(true);
            _cacheService.Setup(x => x.Get<IEnumerable<InsuranceProbatoryDocument>>(_cacheKeyNameForDtoInsuranceProbatoryDocuments)).Returns(cachedInsuranceProbatoryDocuments);
            // Load metadata for insurance probatory documents
            _probatoryDocumentService.Setup(x => x.GetProbatoryDocumentListByIds(It.IsAny<int[]>())).ReturnsAsync(CachedProbatoryDocumentMetadata);

            // Initialize DbSet in context
            _context.Setup(x => x.insurance_probatory_document).ReturnsDbSet(insurance_probatory_document_entities);
            this.SetupContext();

            #endregion

            #region Act

            await service.UpsertInsuranceProbatoryDocuments(insuranceCompanyId, claimTypeId, upsertRequest).ConfigureAwait(false);

            #endregion

            #region Assert

            // Delete the record from db
            var deletedEntity = _context.Object.insurance_probatory_document.FirstOrDefault(entity => entity.insurance_company_id == insuranceCompanyId && entity.claim_type_id == (int)claimTypeId && !upsertRequest.Any(item => item.ProbatoryDocumentId == entity.probatory_document_id));
            _context.Verify(v => v.insurance_probatory_document.Remove(It.Is<insurance_probatory_document>(a => a == deletedEntity)), Times.Once);
            // Delete the record from cache
            var updatedCache = cachedInsuranceProbatoryDocuments.ToList();
            updatedCache.RemoveAll(x => x.InsuranceCompanyId == insuranceCompanyId && x.ClaimTypeId == claimTypeId && x.ProbatoryDocumentId == deletedEntity.probatory_document_id);
            _cacheService.Verify(v => v.Set(_cacheKeyNameForDtoInsuranceProbatoryDocuments, updatedCache), Times.Once);

            #endregion
        }

        #endregion

        #endregion
    }
}
