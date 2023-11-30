using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Moq;
using Moq.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aide.Core.Adapters;
using Microsoft.IdentityModel.Tokens;

namespace Aide.Admin.Services.UnitTests
{
	[TestFixture]
	public class UserServiceTests
	{
		private IJwtSecurityTokenHandlerAdapter _tokenHandler;
		private Mock<IUserPswHistoryService> _userPswHistoryService;
		private Mock<IServiceProvider> _serviceProvider;
		private Mock<ICacheService> _cacheService;
		private Mock<AideDbContext> _context;
		private const string _cacheKeyNameForDtoUsers = "Dto-List-User";
		private const string _symmetricSecurityKey = "symmetric-security-key-for-testing";
		private IUserService service;
		private Random _rnd = new Random();
		private UserService.SecurityLockConfig _securityLock;

		[SetUp]
		public void Setup()
		{
			_tokenHandler = new JwtSecurityTokenHandlerAdapter();
			_serviceProvider = new Mock<IServiceProvider>();
			_userPswHistoryService = new Mock<IUserPswHistoryService>();
			_cacheService = new Mock<ICacheService>();
			_context = new Mock<AideDbContext>();
			_securityLock = new UserService.SecurityLockConfig
			{
				IsEnabled = true,
				MaximumAttempts = 3,
				LockLength = 15,
				TimeFrame = 60
			};
			service = new UserService(_tokenHandler, _serviceProvider.Object, _cacheService.Object, _userPswHistoryService.Object, _securityLock);
			service.SetSymmetricSecurityKey(_symmetricSecurityKey);
		}

		/// <summary>
		/// Setup the context.
		/// Need call this in scenarios that will query the database.
		/// IMPORTANT: The entity initialization must happen before calling this.
		/// </summary>
		private void SetupContext()
		{
			// For passing the DbSet from context to EfRepository
			_context.Setup(x => x.Set<user>()).Returns(_context.Object.user);
			_context.Setup(x => x.Set<user_company>()).Returns(_context.Object.user_company);
			_context.Setup(x => x.Set<user_psw_history>()).Returns(_context.Object.user_psw_history);
			// Initialize context scope
			_serviceProvider.Setup(x => x.GetService(typeof(AideDbContext))).Returns(_context.Object);
			// For initialization of context in EfRepository (it seems like this step is not needed)
			//_serviceProvider.Setup(x => x.GetService(typeof(DbContext))).Returns(_context.Object);
		}

		#region GetAllUsers

		[Test]
		public async Task GetAllUsers_WhenIsFirstTimeCall_ThenPullDataFromDbAndCacheIt()
		{
			#region Arrange

			// These are the mocked records from db
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				},
				new user
				{
					user_id = 4,
					user_first_name = "Store",
					user_last_name = "Operator",
					email = "store_operator@email.com",
					psw = "123",
					role_id = 4,
				},
				new user
				{
					user_id = 5,
					user_first_name = "Double Store",
					user_last_name = "Admin",
					email = "double_store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_company_entities = new List<user_company>
			{
				new user_company
				{
					user_company_id = 1,
					company_id = 0, // For Sys Admin there's no company to associate
					company_type_id = 0, // For Sys Admin there's no company type to associate
					user_id = 1
				},
				new user_company
				{
					user_company_id = 2,
					company_id = 10, // Insurance Id
					company_type_id = 2, // 2-Insurance
					user_id = 2
				},
				new user_company
				{
					user_company_id = 3,
					company_id = 20, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 3
				},
				new user_company
				{
					user_company_id = 4,
					company_id = 20, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 4
				},
				new user_company
				{
					user_company_id = 5,
					company_id = 20, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 5
				},
				new user_company
				{
					user_company_id = 6,
					company_id = 21, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 5
				}
			}; // Metadata

			// When method is called by first time there's nothing in cache
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(false);
			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);
			this.SetupContext();

			#endregion

			#region Act

			var result = await service.GetAllUsers().ConfigureAwait(false);

			#endregion

			#region Assert

			// Look for a cache entry
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoUsers), Times.Once);
			// Since there's no cache then it won't try pull data from there
			_cacheService.Verify(v => v.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers), Times.Never);
			// Initialize a db context twice: 1 time for user(s) and 1 time for user_company(ies) metadata
			_serviceProvider.Verify(v => v.GetService(typeof(AideDbContext)), Times.Exactly(2));
			// Pull data from db one time only
			_context.Verify(v => v.Set<user>(), Times.Once);
			_context.Verify(v => v.Set<user_company>(), Times.Once); // Metadata
																	 // ... and cache the data
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoUsers, It.IsAny<IEnumerable<User>>()), Times.Once);
			// Verify the result is not null
			Assert.IsNotNull(result);
			// Verify the result has the same quantity of items as the number of rows in db
			Assert.AreEqual(result.Count(), user_entities.Count());
			// Verify that all items have metadata
			Assert.IsTrue(!result.Any(x => x.Companies == null));
			Assert.IsTrue(result.Any(x => x.Companies.Any()));

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenIsSecondTimeCallAndGoingForward_ThenPullDataFromCache()
		{
			#region Arrange

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var result = await service.GetAllUsers().ConfigureAwait(false);

			#endregion

			#region Assert

			// Look for a cache entry
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoUsers), Times.Once);
			// It pulls data from cache always
			_cacheService.Verify(v => v.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers), Times.Once);
			// It NEVER initialize a db context scope
			_serviceProvider.Verify(v => v.GetService(typeof(AideDbContext)), Times.Never);
			// so that, it NEVER pulls data from db
			_context.Verify(v => v.Set<user>(), Times.Never);
			_context.Verify(v => v.Set<user_company>(), Times.Never);
			// Verify the result is not null
			Assert.IsNotNull(result);
			// Verify the result has the same quantity of items as the number of rows in db
			Assert.AreEqual(result.Count(), cachedUsers.Count());
			// Verify that all items have metadata
			Assert.IsTrue(!result.Any(x => x.Companies == null));
			Assert.IsTrue(result.Any(x => x.Companies.Any()));

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenPagingSettingsProvided_ThenReturnPagedResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 2
			};
			var filters = new UserService.Filters();
			var viewDetail = EnumViewDetail.Minimum;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one requested
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, pagingSettings.PageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), pagingSettings.PageSize);
			// Verify the total number of pages is correct
			var items = cachedUsers.ToList();
			var expectedPageCount = Math.Ceiling(Convert.ToDecimal(items.Count) / pagingSettings.PageSize);
			Assert.AreEqual(page.PageCount, expectedPageCount);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, items.Count);
			// Verify the results are ordered correctly
			items = items.OrderBy(o1 => o1.FirstName).ThenBy(o2 => o2.LastName).ToList();
			var pagedItems = items.Skip(pagingSettings.PageSize * (pagingSettings.PageNumber - 1)).Take(pagingSettings.PageSize);
			var firstItem = pagedItems.FirstOrDefault();
			var lastItem = pagedItems.LastOrDefault();
			Assert.Multiple(() =>
			{
				Assert.AreEqual(page.Results.FirstOrDefault().Id, firstItem.Id);
				Assert.AreEqual(page.Results.LastOrDefault().Id, lastItem.Id);
			});

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenFilterKeywordsProvidedMatchASingleRecord_ThenReturnFilteredResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 1
			};
			var filters = new UserService.Filters
			{
				Keywords = "operator"
			};
			var viewDetail = EnumViewDetail.Minimum;
			var expectedPageSize = 1; // This is the value expected because there's only 1 item that match the filter

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one expected
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, expectedPageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), expectedPageSize);
			// Verify the total number of pages is correct
			Assert.AreEqual(page.PageCount, pagingSettings.PageNumber);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, expectedPageSize);

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenFilterKeywordsProvidedMatchMultipleRecords_ThenReturnFilteredResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 1
			};
			var filters = new UserService.Filters
			{
				Keywords = "store"
			};
			var viewDetail = EnumViewDetail.Minimum;
			decimal expectedTotalCountOfRecords = 3;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one expected
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, pagingSettings.PageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), pagingSettings.PageSize);
			// Verify the total number of pages is correct
			var items = cachedUsers.ToList();
			var expectedPageCount = Math.Ceiling(expectedTotalCountOfRecords / pagingSettings.PageSize);
			Assert.AreEqual(page.PageCount, expectedPageCount);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, expectedTotalCountOfRecords);

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenFilterCompanyIdAndCompanyTypeIdProvidedMatchASingleRecord_ThenReturnFilteredResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 1
			};
			var filters = new UserService.Filters
			{
				CompanyId = 10,
				CompanyTypeId = EnumCompanyTypeId.Insurance
			};
			var viewDetail = EnumViewDetail.Minimum;
			var expectedPageSize = 1; // This is the value expected because there's only 1 item that match the filter

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one expected
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, expectedPageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), expectedPageSize);
			// Verify the total number of pages is correct
			Assert.AreEqual(page.PageCount, pagingSettings.PageNumber);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, expectedPageSize);

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenFilterCompanyIdAndCompanyTypeIdProvidedMatchMultipleRecords_ThenReturnFilteredResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 1
			};
			var filters = new UserService.Filters
			{
				CompanyId = 20,
				CompanyTypeId = EnumCompanyTypeId.Store
			};
			var viewDetail = EnumViewDetail.Minimum;
			decimal expectedTotalCountOfRecords = 3;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one expected
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, pagingSettings.PageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), pagingSettings.PageSize);
			// Verify the total number of pages is correct
			var items = cachedUsers.ToList();
			var expectedPageCount = Math.Ceiling(expectedTotalCountOfRecords / pagingSettings.PageSize);
			Assert.AreEqual(page.PageCount, expectedPageCount);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, expectedTotalCountOfRecords);

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenFilterCompanyIdProvidedAndCompanyTypeIdNotProvided_ThenReturnUnfilteredPagedResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 1
			};
			var filters = new UserService.Filters
			{
				CompanyId = 10,
				//CompanyTypeId = EnumCompanyTypeId.Insurance // Line commented out in purpose
			};
			var viewDetail = EnumViewDetail.Minimum;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one requested
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, pagingSettings.PageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), pagingSettings.PageSize);
			// Verify the total number of pages is correct
			var expectedPageCount = Math.Ceiling(Convert.ToDecimal(cachedUsers.Count()) / pagingSettings.PageSize);
			Assert.AreEqual(page.PageCount, expectedPageCount);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, cachedUsers.Count());

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenFilterCompanyIdNotProvidedAndCompanyTypeIdProvided_ThenReturnUnfilteredPagedResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 1
			};
			var filters = new UserService.Filters
			{
				//CompanyId = 10, // Line commented out in purpose
				CompanyTypeId = EnumCompanyTypeId.Insurance
			};
			var viewDetail = EnumViewDetail.Minimum;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one requested
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, pagingSettings.PageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), pagingSettings.PageSize);
			// Verify the total number of pages is correct
			var items = cachedUsers.ToList();
			var expectedPageCount = Math.Ceiling(Convert.ToDecimal(items.Count) / pagingSettings.PageSize);
			Assert.AreEqual(page.PageCount, expectedPageCount);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, items.Count);

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenFilterUserRoleIdProvidedMatchASingleRecord_ThenReturnFilteredResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 1
			};
			var filters = new UserService.Filters
			{
				UserRoleIds = new EnumUserRoleId[] { EnumUserRoleId.InsuranceReadOnly }
			};
			var viewDetail = EnumViewDetail.Minimum;
			var expectedPageSize = 1; // This is the value expected because there's only 1 item that match the filter

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one expected
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, expectedPageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), expectedPageSize);
			// Verify the total number of pages is correct
			Assert.AreEqual(page.PageCount, pagingSettings.PageNumber);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, expectedPageSize);

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenFilterUserRoleIdsProvidedMatchMultipleRecords_ThenReturnFilteredResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 1
			};
			var filters = new UserService.Filters
			{
				UserRoleIds = new EnumUserRoleId[] { EnumUserRoleId.Admin, EnumUserRoleId.WsAdmin }
			};
			var viewDetail = EnumViewDetail.Minimum;
			decimal expectedTotalCountOfRecords = 3;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one expected
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, pagingSettings.PageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), pagingSettings.PageSize);
			// Verify the total number of pages is correct
			var items = cachedUsers.ToList();
			var expectedPageCount = Math.Ceiling(expectedTotalCountOfRecords / pagingSettings.PageSize);
			Assert.AreEqual(page.PageCount, expectedPageCount);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, expectedTotalCountOfRecords);

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenViewDetailProvidedIsMinimum_ThenReturnViewDetailResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 1
			};
			var filters = new UserService.Filters();
			var viewDetail = EnumViewDetail.Minimum;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one requested
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, pagingSettings.PageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), pagingSettings.PageSize);
			// Verify the total number of pages is correct
			var items = cachedUsers.ToList();
			var expectedPageCount = Math.Ceiling(Convert.ToDecimal(items.Count) / pagingSettings.PageSize);
			Assert.AreEqual(page.PageCount, expectedPageCount);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, items.Count);
			// Verify the view detail is valid
			Assert.IsTrue(!page.Results.Any(x => x.Companies != null));

			#endregion
		}

		[Test]
		public async Task GetAllUsers_WhenViewDetailProvidedIsExtended_ThenReturnViewDetailResult()
		{
			#region Arrange

			var pagingSettings = new PagingSettings
			{
				PageSize = 2,
				PageNumber = 1
			};
			var filters = new UserService.Filters();
			var viewDetail = EnumViewDetail.Extended;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var page = await service.GetAllUsers(pagingSettings, filters, viewDetail).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the page results is not null
			Assert.IsNotNull(page.Results);
			// Verify the resulting page is the one requested
			Assert.AreEqual(page.CurrentPage, pagingSettings.PageNumber);
			// Verify the size of the page is correct
			Assert.AreEqual(page.PageSize, pagingSettings.PageSize);
			// Verify the page results have items
			Assert.IsTrue(page.Results.Any());
			// Verify the number of page results is correct
			Assert.AreEqual(page.Results.Count(), pagingSettings.PageSize);
			// Verify the total number of pages is correct
			var items = cachedUsers.ToList();
			var expectedPageCount = Math.Ceiling(Convert.ToDecimal(items.Count) / pagingSettings.PageSize);
			Assert.AreEqual(page.PageCount, expectedPageCount);
			// Verify the total count of items for all pages is correct
			Assert.AreEqual(page.RowCount, items.Count);
			// Verify the view detail is valid
			Assert.IsTrue(!page.Results.Any(x => x.Companies == null));
			Assert.IsTrue(page.Results.Any(x => x.Companies.Any()));

			#endregion
		}

		#endregion

		#region GetUserById

		[Test]
		public async Task GetUserById_WhenValidUserIdProvided_ThenReturnUser()
		{
			#region Arrange

			var validUserId = 1;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var result = await service.GetUserById(validUserId).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			// Verify the ID is correct
			Assert.AreEqual(result.Id, validUserId);
			// Verify metadata is included
			Assert.IsNotNull(result.Companies);
			Assert.IsTrue(result.Companies.Any());

			#endregion
		}

		[Test]
		public void GetUserById_WhenInvalidUserIdProvided_ThenThrowNonExistingRecordCustomizedException()
		{
			#region Arrange

			var invalidUserId = 99;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			#endregion

			#region Assert

			Assert.That(() => service.GetUserById(invalidUserId), Throws.Exception.TypeOf<NonExistingRecordCustomizedException>());

			#endregion
		}

		[Test]
		public void GetUserById_WhenTheyAreNotRecordsAtAll_ThenThrowNonExistingRecordCustomizedException()
		{
			#region Arrange

			var validUserId = 1;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>();
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			#endregion

			#region Assert

			Assert.That(() => service.GetUserById(validUserId), Throws.Exception.TypeOf<NonExistingRecordCustomizedException>());

			#endregion
		}

		#endregion

		#region GetUserByEmail

		[Test]
		public async Task GetUserByEmail_WhenValidEmailAddressProvided_ThenReturnUser()
		{
			#region Arrange

			var validEmailAddress = "admin@email.com";

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var result = await service.GetUserByEmail(validEmailAddress).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			// Verify the ID is correct
			Assert.AreEqual(result.Email, validEmailAddress);
			// Verify metadata is included
			Assert.IsNotNull(result.Companies);
			Assert.IsTrue(result.Companies.Any());

			#endregion
		}

		[Test]
		public async Task GetUserByEmail_WhenValidEmailAddressProvidedAndTheEmailAddressIsInDifferentCasing_ThenReturnUser()
		{
			#region Arrange

			var validEmailAddress = "Admin@EMAIL.com";

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var result = await service.GetUserByEmail(validEmailAddress).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			// Verify the ID is correct
			//Assert.AreEqual(result.Email, validEmailAddress);
			Assert.AreEqual(0, string.Compare(result.Email, validEmailAddress, true));
			// Verify metadata is included
			Assert.IsNotNull(result.Companies);
			Assert.IsTrue(result.Companies.Any());

			#endregion
		}

		[Test]
		public void GetUserByEmail_WhenInvalidEmailAddressProvided_ThenThrowNonExistingRecordCustomizedException()
		{
			#region Arrange

			var invalidEmailAddress = "invalid@email.com";

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			#endregion

			#region Assert

			Assert.That(() => service.GetUserByEmail(invalidEmailAddress), Throws.Exception.TypeOf<NonExistingRecordCustomizedException>());

			#endregion
		}

		[Test]
		public void GetUserByEmail_WhenTheyAreNotRecordsAtAll_ThenThrowNonExistingRecordCustomizedException()
		{
			#region Arrange

			var validEmailAddress = "admin@email.com";

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>();
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			#endregion

			#region Assert

			Assert.That(() => service.GetUserByEmail(validEmailAddress), Throws.Exception.TypeOf<NonExistingRecordCustomizedException>());

			#endregion
		}

		#endregion

		#region GetUserListByUserIds

		[Test]
		public async Task GetUserListByUserIds_WhenListOfValidUserIdsProvided_ThenReturnListOfUsers()
		{
			#region Arrange

			var validUserIds = new int[] { 1, 2 };

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var result = await service.GetUserListByUserIds(validUserIds).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			// Verify the result has the same quantity of items as the number of valid user IDs provided
			Assert.AreEqual(result.Count(), validUserIds.Count());
			// Verify that all items have metadata
			Assert.IsTrue(!result.Any(x => x.Companies == null));
			Assert.IsTrue(result.Any(x => x.Companies.Any()));

			#endregion
		}

		[Test]
		public async Task GetUserListByUserIds_WhenListOfValidAndInvalidUserIdsProvided_ThenReturnListOfValidUsers()
		{
			#region Arrange

			var validAndInvalidUserIds = new int[] { 1, 2, 99 }; // 99 is a non-existing user
			var validUsersCount = 2; // This is because 99 is a non-existing user

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var result = await service.GetUserListByUserIds(validAndInvalidUserIds).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			// Verify the result has the same quantity of items as the number of valid user IDs provided
			Assert.AreEqual(result.Count(), validUsersCount);
			// Verify that all items have metadata
			Assert.IsTrue(!result.Any(x => x.Companies == null));
			Assert.IsTrue(result.Any(x => x.Companies.Any()));

			#endregion
		}

		[Test]
		public void GetUserListByUserIds_WhenListOfInvalidUserIdsProvided_ThenThrowNonExistingRecordCustomizedException()
		{
			#region Arrange

			var invalidUserIds = new int[] { 99, 100 };

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			#endregion

			#region Assert

			Assert.That(() => service.GetUserListByUserIds(invalidUserIds), Throws.Exception.TypeOf<NonExistingRecordCustomizedException>());

			#endregion
		}

		[Test]
		public void GetUserListByUserIds_WhenTheyAreNotRecordsAtAll_ThenThrowNonExistingRecordCustomizedException()
		{
			#region Arrange

			var validUserIds = new int[] { 1, 2 };

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>();
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			#endregion

			#region Assert

			Assert.That(() => service.GetUserListByUserIds(validUserIds), Throws.Exception.TypeOf<NonExistingRecordCustomizedException>());

			#endregion
		}

		#endregion

		#region Authenticate

		[Test]
		public async Task Authenticate_WhenValidUserAndPswProvided_ThenReturnUserAuthWithJwtIncluded()
		{
			#region Arrange

			var validUsr = "admin@email.com";
			var validPsw = "123";

			//The functionality securityLock is OFF
			_securityLock.IsEnabled = false;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// This is the record in DB that will require an update upon the login attempt
			var user_entity = new user
            {
                user_id = 1,
                user_first_name = "Sys",
                user_last_name = "Admin",
                email = "admin@email.com",
                psw = "123",
                role_id = 1
            };

            // Initialize DbSet in context
            _context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
            this.SetupContext();

			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var result = await service.Authenticate(validUsr, validPsw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is NOT null
			Assert.IsNotNull(result);
			// Verify a JWT exist
			Assert.IsNotNull(result.Token);

			#endregion
		}

		[Test]
		public async Task Authenticate_WhenValidUserAndPswAreProvidedAndTheUserIsInCapitalCasing_ThenReturnUserAuthWithJwtIncluded()
		{
			#region Arrange

			var validUsr = "AdMiN@eMaIl.Com";
			var validPsw = "123";

			//The functionality securityLock is OFF
			_securityLock.IsEnabled = false;

			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// This is the record in DB that will require an update upon the login attempt
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var result = await service.Authenticate(validUsr, validPsw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is NOT null
			Assert.IsNotNull(result);
			// Verify a JWT exist
			Assert.IsNotNull(result.Token);

			#endregion
		}

		[Test]
		public async Task Authenticate_WhenInvalidUserProvided_ThenReturnNull()
		{
			#region Arrange

			var invalidUsr = "invalid@email.com";
			var valirPsw = "123";

			//The functionality securityLock is OFF
			_securityLock.IsEnabled = false;

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_company_entities = new List<user_company>();
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				},
				new user
				{
					user_id = 4,
					user_first_name = "Store",
					user_last_name = "Operator",
					email = "store_operator@email.com",
					psw = "123",
					role_id = 4,
				},
				new user
				{
					user_id = 5,
					user_first_name = "Double Store",
					user_last_name = "Admin",
					email = "double_store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);
			this.SetupContext();

			#endregion

			#region Act

			var result = await service.Authenticate(invalidUsr, valirPsw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is null
			Assert.IsNull(result);

			#endregion
		}

		[Test]
		public async Task Authenticate_WhenInvalidPswProvided_ThenReturnNull()
		{
			#region Arrange

			var validUsr = "invalid@email.com";
			var invalirPsw = "invalid";

			//The functionality securityLock is OFF
			_securityLock.IsEnabled = false;

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_company_entities = new List<user_company>();
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				},
				new user
				{
					user_id = 4,
					user_first_name = "Store",
					user_last_name = "Operator",
					email = "store_operator@email.com",
					psw = "123",
					role_id = 4,
				},
				new user
				{
					user_id = 5,
					user_first_name = "Double Store",
					user_last_name = "Admin",
					email = "double_store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);
			this.SetupContext();

			#endregion

			#region Act

			var result = await service.Authenticate(validUsr, invalirPsw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is null
			Assert.IsNull(result);

			#endregion
		}

		[Test]
		public async Task Authenticate_WhenSecurityLockIsOnAndAValidPasswordIsProvided_ThenReturnUserAuthWithJwtIncluded()
		{
			#region Arrange

			var validUsr = "admin@email.com";
			var validPsw = "123";

			//Cuando el password es valido y es la primera vez en intentar autentificarse, debe ser exitoso el login
			int? userLoginAttempt = null;
			DateTime? userDateLastAttempt = null;

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_company_entities = new List<user_company>();
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
					last_login_attempt = userLoginAttempt,
					time_last_attempt = userDateLastAttempt
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1,
				last_login_attempt = userLoginAttempt,
				time_last_attempt = userDateLastAttempt
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			#endregion

			#region Act

			var result = await service.Authenticate(validUsr, validPsw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			Assert.IsNotNull(result.Token);
			Assert.IsTrue(string.IsNullOrWhiteSpace(result.Message));
			Assert.IsTrue(result.IsLoginSuccessful);

			#endregion
		}

		[Test]
		public async Task Authenticate_WhenSecurityLockIsOnAndAValidPasswordIsProvidedAndTheAccountIsLocked_ThenReturnUserAuthWithErrorMessage()
		{
			#region Arrange

			var validUsr = "admin@email.com";
			var validPsw = "123";

			//Cuando el password es valido, el numero de intentos son 4, el usuario debe estar bloqueado
			var userLoginAttempt = _securityLock.MaximumAttempts + 1;
			DateTime userDateLastAttempt = DateTime.UtcNow;

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_company_entities = new List<user_company>();
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
					last_login_attempt = userLoginAttempt
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1,
				last_login_attempt = userLoginAttempt
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			#endregion

			#region Act

			var result = await service.Authenticate(validUsr, validPsw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			Assert.IsNull(result.Token);
			Assert.IsTrue(result.IsUserLocked);
			Assert.IsTrue(!result.IsLoginSuccessful);
			Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));

			#endregion
		}

		[Test]
		public async Task Authenticate_WhenSecurityLockIsOnAndAValidPasswordIsProvidedAndTheLockOnTheAccountIsExpired_ThenReturnUserAuthWithJwtIncluded()
		{
			#region Arrange

			var validUsr = "admin@email.com";
			var validPsw = "123";

			//El usuario se desbloquea después de que se completa la cantidad de tiempo especificada
			var userLoginAttempt = _securityLock.MaximumAttempts + 1;
			DateTime userDateLastAttempt = DateTime.UtcNow.AddMinutes(_securityLock.LockLength + 3);

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_company_entities = new List<user_company>();
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
					last_login_attempt = userLoginAttempt,
					time_last_attempt = userDateLastAttempt
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1,
				last_login_attempt = userLoginAttempt,
				time_last_attempt = userDateLastAttempt
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			#endregion

			#region Act

			var result = await service.Authenticate(validUsr, validPsw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			Assert.IsNotNull(result.Token);
			Assert.IsTrue(string.IsNullOrWhiteSpace(result.Message));
			Assert.IsTrue(!result.IsUserLocked);
			Assert.IsTrue(result.IsLoginSuccessful);

			#endregion
		}

		[Test]
		public async Task Authenticate_WhenSecurityLockIsOnAndTheMaxNumberOfAttemptsIsReached_ThenLockTheUserAccount()
		{
			#region Arrange

			var validUsr = "admin@email.com";
			var invalidPsw = "1234";

			//Cuando el password es invalido, el numero de intentos son 3 y ya llego al maximo de intentos, el usuario debe estar bloqueado
			var userLoginAttempt = _securityLock.MaximumAttempts;
			DateTime userDateLastAttempt = DateTime.UtcNow;

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_company_entities = new List<user_company>();
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
					last_login_attempt = userLoginAttempt
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1,
				last_login_attempt = userLoginAttempt
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			#endregion

			#region Act

			var result = await service.Authenticate(validUsr, invalidPsw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			Assert.IsNull(result.Token);
			Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Message));
			Assert.IsTrue(result.IsUserLocked);

			#endregion
		}

		[Test]
		public async Task Authenticate_WhenSecurityLockIsOnAndTheAccountIsLockedAndAnInvalidPasswordIsProvided_ThenDoNotExtendTheLengthOfTheLock()
		{
			#region Arrange

			var validUsr = "admin@email.com";
			var validPsw = "123";

			//Cuando el password es valido, el numero de intentos ya son 4, pero el periodo de tiempo para reiniciar variables es de 61 min ya paso
			//Debe ser exitoso el login 
			var userLoginAttempt = _securityLock.MaximumAttempts + 1;
			var userDateLastAttempt = DateTime.UtcNow.AddMinutes(_securityLock.TimeFrame + 1);

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_company_entities = new List<user_company>();
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
					last_login_attempt = userLoginAttempt,
					time_last_attempt = userDateLastAttempt
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1,
				last_login_attempt = userLoginAttempt,
				time_last_attempt = userDateLastAttempt
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			#endregion

			#region Act

			var result = await service.Authenticate(validUsr, validPsw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			Assert.IsNotNull(result.Token);
			Assert.IsTrue(string.IsNullOrWhiteSpace(result.Message));
			Assert.IsTrue(result.IsLoginSuccessful);

			#endregion
		}

		[Test]
		public async Task Authenticate_WhenSecurityLockIsOffAndAnInvalidPasswordIsProvided_ThenDoNotLockTheUserAccountRegardlessOfTheNumberOfFailedLoginAttempts()
		{
			#region Arrange

			var validUsr = "admin@email.com";
			var invalidPsw = "1234";

			//Cuando el password es invalido, aunque las variables rebasen los maximos si esta apagada la funcionalidad 
			//No debe bloquear al usuario
			var userLoginAttempt = _securityLock.MaximumAttempts;
			var userDateLastAttempt = DateTime.UtcNow;
			_securityLock.IsEnabled = false;

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_company_entities = new List<user_company>();
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
					last_login_attempt = userLoginAttempt,
					time_last_attempt = userDateLastAttempt
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1,
				last_login_attempt = userLoginAttempt,
				time_last_attempt = userDateLastAttempt
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			#endregion

			#region Act

			var result = await service.Authenticate(validUsr, invalidPsw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the result is not null
			Assert.IsNotNull(result);
			Assert.IsNull(result.Token);
			Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Message));
			Assert.IsTrue(!result.IsUserLocked);
			Assert.IsTrue(!result.IsLoginSuccessful);

			#endregion
		}

		#endregion

		#region ReadKeyFromJwtToken

		[Test]
		public async Task ReadKeyFromJwtToken_WhenValidTokenAndKeyProvided_ThenReturnKeyValue()
		{
			#region Arrange

			// Since the scope of this test is reading a key from the token I will disable the security lock
			_securityLock.IsEnabled = false;

			var validUsr = "admin@email.com";
			var validPsw = "123";

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				},
				new user
				{
					user_id = 4,
					user_first_name = "Store",
					user_last_name = "Operator",
					email = "store_operator@email.com",
					psw = "123",
					role_id = 4,
				},
				new user
				{
					user_id = 5,
					user_first_name = "Double Store",
					user_last_name = "Admin",
					email = "double_store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			// Once the user and password have been validated the rest of the data is pulled either from db or cache... for practicity here we'll use cache
			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var userAuth = await service.Authenticate(validUsr, validPsw).ConfigureAwait(false);
			var nameid = service.ReadKeyFromJwtToken(userAuth.Token, "nameid");
			var givenName = service.ReadKeyFromJwtToken(userAuth.Token, "given_name");
			var familyName = service.ReadKeyFromJwtToken(userAuth.Token, "family_name");
			var email = service.ReadKeyFromJwtToken(userAuth.Token, "email");
			var role = service.ReadKeyFromJwtToken(userAuth.Token, "role");
			var companies = service.ReadKeyFromJwtToken(userAuth.Token, "companies");

			#endregion

			#region Assert

			var user = cachedUsers.FirstOrDefault(x => x.Email == validUsr);
			var userCompanies = string.Join(",", user.Companies.Select(x => x.CompanyId).ToArray());

			Assert.Multiple(() =>
			{
				Assert.NotNull(nameid);
				Assert.AreEqual(nameid, user.Id.ToString());
				Assert.NotNull(givenName);
				Assert.AreEqual(givenName, user.FirstName);
				Assert.NotNull(familyName);
				Assert.AreEqual(familyName, user.LastName);
				Assert.NotNull(email);
				Assert.AreEqual(email, user.Email);
				Assert.NotNull(role);
				Assert.AreEqual(role, ((int)user.RoleId).ToString());
				Assert.NotNull(companies);
				Assert.AreEqual(companies, userCompanies);
			});

			#endregion
		}

		[Test]
		public async Task ReadKeyFromJwtToken_WhenInvalidKeyProvided_ThenReturnNull()
		{
			#region Arrange

			// Since the scope of this test is reading a key from the token I will disable the security lock
			_securityLock.IsEnabled = false;

			var validUsr = "admin@email.com";
			var validPsw = "123";

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				},
				new user
				{
					user_id = 4,
					user_first_name = "Store",
					user_last_name = "Operator",
					email = "store_operator@email.com",
					psw = "123",
					role_id = 4,
				},
				new user
				{
					user_id = 5,
					user_first_name = "Double Store",
					user_last_name = "Admin",
					email = "double_store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			// Once the user and password have been validated the rest of the data is pulled either from db or cache... for practicity here we'll use cache
			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var userAuth = await service.Authenticate(validUsr, validPsw).ConfigureAwait(false);
			var keyValue = service.ReadKeyFromJwtToken(userAuth.Token, "invalid-key");

			#endregion

			#region Assert

			Assert.IsNull(keyValue);

			#endregion
		}

		[Test]
		public void ReadKeyFromJwtToken_WhenInalidTokenProvided_ThenThrowArgumentException()
		{
			#region Arrange

			var invalidToken = "invalid-token";

			#endregion

			#region Act

			#endregion

			#region Assert

			Assert.That(() => service.ReadKeyFromJwtToken(invalidToken, "nameid"), Throws.Exception.TypeOf<SecurityTokenMalformedException>());

			#endregion
		}

		#endregion

		#region UserLogout

		[Test]
		public async Task UserLogout_WhenValidUserIdProvided_ThenUpdateLogoutDate()
		{
			#region Arrange

			var validUserId = 1;
			var rndNumberOfDays = _rnd.Next(1, 7);
			var previousLogOutDate = DateTime.UtcNow.AddDays(-rndNumberOfDays); // Last logout happened a random number of days ago

			// This is a single mocked record from db
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1,
				date_logout = previousLogOutDate
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user.FindAsync(validUserId)).ReturnsAsync(user_entity);
			this.SetupContext();

			#endregion

			#region Act

			await service.UserLogout(validUserId).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify the update on log out date is persisted
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
			// Verify the log out date has changed
			var totalDays = Math.Round((user_entity.date_logout - previousLogOutDate).TotalDays);
			Assert.AreEqual(totalDays, rndNumberOfDays);

			#endregion
		}

		[Test]
		public void UserLogout_WhenInvalidUserIdProvided_ThenThrowNonExistingRecordCustomizedException()
		{
			#region Arrange

			var invalidUserId = 99;

			// Initialize DbSet in context
			_context.Setup(x => x.user.FindAsync(invalidUserId)).ReturnsAsync((user)null);
			this.SetupContext();

			#endregion

			#region Act

			#endregion

			#region Assert

			Assert.That(() => service.UserLogout(invalidUserId), Throws.Exception.TypeOf<NonExistingRecordCustomizedException>());

			#endregion
		}

		#endregion

		#region BuildUserFromJwtToken

		[Test]
		public async Task BuildUserFromJwtToken_WhenValidTokenProvided_ThenReturnUser()
		{
			#region Arrange

			// Since the scope of this test is reading a token I will disable the security lock
			_securityLock.IsEnabled = false;

			var validUsr = "double_store_admin@email.com";
			var validPsw = "123";

			//The functionality securityLock is OFF
			_securityLock.IsEnabled = false;

			// For user and password validation it will ALWAYS query the database
			// These are the mocked records from db
			var user_company_entities = new List<user_company>();
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				},
				new user
				{
					user_id = 4,
					user_first_name = "Store",
					user_last_name = "Operator",
					email = "store_operator@email.com",
					psw = "123",
					role_id = 4,
				},
				new user
				{
					user_id = 5,
					user_first_name = "Double Store",
					user_last_name = "Admin",
					email = "double_store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			// Once the user and password have been validated the rest of the data is pulled either from db or cache... for practicity here we'll use cache
			// To keep this scenario simple we will assume the data is not coming from database but cache
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			var authUser = await service.Authenticate(validUsr, validPsw).ConfigureAwait(false);
			var result = service.BuildUserFromJwtToken(authUser.Token);

			#endregion

			#region Assert

			// Verify the result is NOT null
			Assert.NotNull(result);
			// Verify the user generated is correct
			var expected = user_entities.FirstOrDefault(x => x.email == validUsr);
			Assert.Multiple(() =>
			{
				Assert.AreEqual(result.Id, expected.user_id);
				Assert.AreEqual(result.RoleId, (EnumUserRoleId)expected.role_id);
				Assert.AreEqual(result.FirstName, expected.user_first_name);
				Assert.AreEqual(result.LastName, expected.user_last_name);
				Assert.AreEqual(result.Email, expected.email);
				Assert.AreEqual(result.DateCreated, expected.date_created);
				Assert.AreEqual(result.DateLogout, expected.date_logout);
				Assert.NotNull(result.Companies);
				Assert.IsTrue(result.Companies.Any());
			});

			#endregion
		}

		[Test]
		public void BuildUserFromJwtToken_WhenInvalidTokenProvided_ThenThrowArgumentException()
		{
			#region Arrange

			var invalidToken = "invalid-token";

			#endregion

			#region Act

			#endregion

			#region Assert

			Assert.That(() => service.BuildUserFromJwtToken(invalidToken), Throws.Exception.TypeOf<SecurityTokenMalformedException>());

			#endregion
		}

		#endregion

		#region InsertUser

		[Test]
		public async Task InsertUser_WhenUserProvidedWithoutCompanies_ThenInsertUserWithoutCompanies()
		{
			#region Arrange

			var newUser = new User
			{
				Email = "new_user@email.com",
				FirstName = "New",
				LastName = "User",
				RoleId = EnumUserRoleId.Admin,
				Companies = new List<UserCompany>()
			};

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			// These are the mocked records from db
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				},
				new user
				{
					user_id = 4,
					user_first_name = "Store",
					user_last_name = "Operator",
					email = "store_operator@email.com",
					psw = "123",
					role_id = 4,
				},
				new user
				{
					user_id = 5,
					user_first_name = "Double Store",
					user_last_name = "Admin",
					email = "double_store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_company_entities = new List<user_company>
			{
				new user_company
				{
					user_company_id = 1,
					company_id = 0, // For Sys Admin there's no company to associate
					company_type_id = 0, // For Sys Admin there's no company type to associate
					user_id = 1
				},
				new user_company
				{
					user_company_id = 2,
					company_id = 10, // Insurance Id
					company_type_id = 2, // 2-Insurance
					user_id = 2
				},
				new user_company
				{
					user_company_id = 3,
					company_id = 20, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 3
				},
				new user_company
				{
					user_company_id = 4,
					company_id = 20, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 4
				},
				new user_company
				{
					user_company_id = 5,
					company_id = 20, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 5
				},
				new user_company
				{
					user_company_id = 6,
					company_id = 21, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 5
				}
			}; // Metadata

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);

			// Setup auto increment on user.user_id
			_context.Setup(x => x.user.Add(It.IsAny<user>())).Callback<user>((new_entity) =>
			{
				var nextId = user_entities.Count() + 1;
				new_entity.user_id = nextId;
				new_entity.date_created = DateTime.UtcNow;
				new_entity.date_modified = DateTime.UtcNow;
				user_entities.Add(new_entity);
			});

			this.SetupContext();

			#endregion

			#region Act

			var result = await service.InsertUser(newUser).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify that a new user entity was added to the context
			_context.Verify(v => v.user.Add(It.IsAny<user>()), Times.Once);
			// Verify that no one user_company entity was added to the context
			_context.Verify(v => v.user_company.Add(It.IsAny<user_company>()), Times.Never);
			// Verify the db transaction was commited
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
			// Verify the cache was updated
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoUsers), Times.AtLeastOnce);
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoUsers, It.IsAny<object>()), Times.Once);
			// Verify that a news entity was added to the context
			_userPswHistoryService.Verify(v => v.InsertPasswordHistory(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
			// Verify the result is NOT null
			Assert.NotNull(result);
			// Verify a password has been automatically generated
			Assert.NotNull(result.TemporaryPassword);
			// Verify a hash password has been automatically assigned
			Assert.NotNull(result.User.Psw);
			// Verify that no companies where assigned
			Assert.IsFalse(result.User.Companies.Any());

			#endregion
		}

		[Test]
		public async Task InsertUser_WhenUserProvidedWithCompanies_ThenInsertUserWithCompanies()
		{
			#region Arrange

			var newUser = new User
			{
				Email = "new_user@email.com",
				FirstName = "New",
				LastName = "User",
				RoleId = EnumUserRoleId.InsuranceReadOnly,
				Companies = new List<UserCompany>
				{
					new UserCompany
					{
						CompanyId = 10,
						CompanyTypeId = EnumCompanyTypeId.Insurance
					}
				}
			};

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			// These are the mocked records from db
			var user_entities = new List<user>
			{
				new user
				{
					user_id = 1,
					user_first_name = "Sys",
					user_last_name = "Admin",
					email = "admin@email.com",
					psw = "123",
					role_id = 1,
				},
				new user
				{
					user_id = 2,
					user_first_name = "Insurance",
					user_last_name = "Rean-only",
					email = "insurance@email.com",
					psw = "123",
					role_id = 2,
				},
				new user
				{
					user_id = 3,
					user_first_name = "Store",
					user_last_name = "Admin",
					email = "store_admin@email.com",
					psw = "123",
					role_id = 3,
				},
				new user
				{
					user_id = 4,
					user_first_name = "Store",
					user_last_name = "Operator",
					email = "store_operator@email.com",
					psw = "123",
					role_id = 4,
				},
				new user
				{
					user_id = 5,
					user_first_name = "Double Store",
					user_last_name = "Admin",
					email = "double_store_admin@email.com",
					psw = "123",
					role_id = 3,
				}
			};
			var user_company_entities = new List<user_company>
			{
				new user_company
				{
					user_company_id = 1,
					company_id = 0, // For Sys Admin there's no company to associate
					company_type_id = 0, // For Sys Admin there's no company type to associate
					user_id = 1
				},
				new user_company
				{
					user_company_id = 2,
					company_id = 10, // Insurance Id
					company_type_id = 2, // 2-Insurance
					user_id = 2
				},
				new user_company
				{
					user_company_id = 3,
					company_id = 20, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 3
				},
				new user_company
				{
					user_company_id = 4,
					company_id = 20, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 4
				},
				new user_company
				{
					user_company_id = 5,
					company_id = 20, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 5
				},
				new user_company
				{
					user_company_id = 6,
					company_id = 21, // Store Id
					company_type_id = 1, // 1-Store
					user_id = 5
				}
			}; // Metadata

			// Initialize DbSet in context
			_context.Setup(x => x.user).ReturnsDbSet(user_entities);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);

			// Setup auto increment on user.user_id
			_context.Setup(x => x.user.Add(It.IsAny<user>())).Callback<user>((new_entity) =>
			{
				var nextId = user_entities.Count() + 1;
				new_entity.user_id = nextId;
				new_entity.date_created = DateTime.UtcNow;
				new_entity.date_modified = DateTime.UtcNow;
				user_entities.Add(new_entity);
			});

			this.SetupContext();

			#endregion

			#region Act

			var result = await service.InsertUser(newUser).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify that a new user entity was added to the context
			_context.Verify(v => v.user.Add(It.IsAny<user>()), Times.Once);
			// Verify that a new user_company entity was added to the context
			_context.Verify(v => v.user_company.Add(It.IsAny<user_company>()), Times.Once);
			// Verify the db transaction was commited: 1 time for user and another one for user_company
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
			// Verify the cache was updated
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoUsers), Times.AtLeastOnce);
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoUsers, It.IsAny<object>()), Times.Once);
			// Verify that a news entity was added to the context
			_userPswHistoryService.Verify(v => v.InsertPasswordHistory(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
			// Verify the result is NOT null
			Assert.NotNull(result);
			// Verify a password has been automatically generated
			Assert.NotNull(result.TemporaryPassword);
			// Verify a hash password has been automatically assigned
			Assert.NotNull(result.User.Psw);
			// Verify that companies where assigned
			Assert.IsTrue(result.User.Companies.Any());

			#endregion
		}

		[Test]
		public void InsertUser_WhenTheEmailAddressIsBeingUsedByAnExistingUser_ThenThrowDuplicatedRecordCustomizedException()
		{
			#region Arrange

			var newUser = new User
			{
				Email = "insurance@email.com"
			};

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 1,
							CompanyId = 0,
							CompanyTypeId = EnumCompanyTypeId.Unknown,
							UserId = 1
						}
					}
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			#endregion

			#region Act

			#endregion

			#region Assert

			// Verify that a news entity was added to the context
			_userPswHistoryService.Verify(v => v.InsertPasswordHistory(It.IsAny<int>(), It.IsAny<string>()), Times.Never);

			Assert.That(() => service.InsertUser(newUser), Throws.Exception.TypeOf<DuplicatedRecordCustomizedException>());

			#endregion
		}

		#endregion

		#region UpdateUser

		[Test]
		public async Task UpdateUser_WhenUpdatedUserProvided_ThenUpdateUser()
		{
			#region Arrange

			var userUpdate = new User
			{
				Id = 1,
				Email = "admin@email.com",
				Psw = null,
				FirstName = "Updated",
				LastName = "Updated",
				RoleId = EnumUserRoleId.Admin,
				Companies = new List<UserCompany>()
			};

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>()
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			// This is a single mocked record from db
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);

			this.SetupContext();

			#endregion

			#region Act

			await service.UpdateUser(userUpdate).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify a query executed to find the user
			_context.Verify(v => v.user.FindAsync(It.IsAny<object>()), Times.Once);
			// Verify that a user_company has NEVER been added to context
			_context.Verify(v => v.user_company.Add(It.IsAny<user_company>()), Times.Never);
			// Verify that a user_company has NEVER been removed from context
			_context.Verify(v => v.user_company.Remove(It.IsAny<user_company>()), Times.Never);
			// Verify the user update has been commited
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
			// Verify the cache has been updated
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoUsers), Times.AtLeastOnce);
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoUsers, It.IsAny<object>()), Times.Once);

			#endregion
		}

		[Test]
		public async Task UpdateUser_WhenAddedCompany_ThenUpdateUser()
		{
			#region Arrange

			var userUpdate = new User
			{
				Id = 5,
				Email = "double_store_admin@email.com",
				Psw = null,
				FirstName = "Double Store",
				LastName = "Admin",
				RoleId = EnumUserRoleId.WsAdmin,
				Companies = new List<UserCompany>
				{
					new UserCompany
					{
						Id = 5,
						CompanyId = 20,
						CompanyTypeId = EnumCompanyTypeId.Store,
						UserId = 5
					},
					new UserCompany
					{
						// Noticed there's no value for Id because this is a company that is being added
						CompanyId = 21,
						CompanyTypeId = EnumCompanyTypeId.Store,
						UserId = 5
					}
				}
			};

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>()
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			// This is a single mocked record from db
			var user_entity = new user
			{
				user_id = 5,
				user_first_name = "Double Store",
				user_last_name = "Admin",
				email = "double_store_admin@email.com",
				psw = "123",
				//psw = "123", // Notice that psw is not mandatory when updating and existing user
				role_id = 3
			};
			var user_company_entity = new user_company
			{
				user_company_id = 5,
				company_id = 20,
				company_type_id = 1,
				user_id = 5
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			_context.Setup(x => x.user_company.FindAsync(It.IsAny<object>())).ReturnsAsync(user_company_entity);

			this.SetupContext();

			#endregion

			#region Act

			await service.UpdateUser(userUpdate).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify a query executed to find the user
			_context.Verify(v => v.user.FindAsync(It.IsAny<object>()), Times.Once);
			// Verify that a user_company has been added to context
			_context.Verify(v => v.user_company.Add(It.IsAny<user_company>()), Times.Once);
			// Verify that a user_company has NEVER been removed from context
			_context.Verify(v => v.user_company.Remove(It.IsAny<user_company>()), Times.Never);
			// Verify the user update user_company insertion have BOTH been committed
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
			// Verify the cache has been updated
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoUsers), Times.AtLeastOnce);
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoUsers, It.IsAny<object>()), Times.Once);

			#endregion
		}

		[Test]
		public async Task UpdateUser_WhenRemovedCompany_ThenUpdateUser()
		{
			#region Arrange

			var userUpdate = new User
			{
				Id = 5,
				Email = "double_store_admin@email.com",
				Psw = null,
				FirstName = "Double Store",
				LastName = "Admin",
				RoleId = EnumUserRoleId.WsAdmin,
				Companies = new List<UserCompany>
				{
					new UserCompany
					{
						Id = 5,
						CompanyId = 20,
						CompanyTypeId = EnumCompanyTypeId.Store,
						UserId = 5
					},
					// The commented company below will be removed from this user
					//new UserCompany
					//{
					//	Id = 6,
					//	CompanyId = 21,
					//	CompanyTypeId = EnumCompanyTypeId.Store,
					//	UserId = 5
					//}
				}
			};

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>()
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			// These are mocked records from db
			var user_entity = new user
			{
				user_id = 5,
				user_first_name = "Double Store",
				user_last_name = "Admin",
				email = "double_store_admin@email.com",
				psw = "123",
				role_id = 3
			};
			var user_company_entities = new List<user_company>
			{
				new user_company
				{
					user_company_id = 5,
					company_id = 20,
					company_type_id = 1,
					user_id = 5
				},
				new user_company
				{
					user_company_id = 6,
					company_id = 21,
					company_type_id = 1,
					user_id = 5
				}
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			_context.Setup(x => x.user_company).ReturnsDbSet(user_company_entities);

			this.SetupContext();

			#endregion

			#region Act

			await service.UpdateUser(userUpdate).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify a query executed to find the user
			_context.Verify(v => v.user.FindAsync(It.IsAny<object>()), Times.Once);
			// Verify that a user_company has NEVER been added to context
			_context.Verify(v => v.user_company.Add(It.IsAny<user_company>()), Times.Never);
			// Verify that a user_company has been removed from context
			_context.Verify(v => v.user_company.Remove(It.IsAny<user_company>()), Times.Once);
			// Verify the user update and user_company deletion have BOTH been commited
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
			// Verify the cache has been updated
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoUsers), Times.AtLeastOnce);
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoUsers, It.IsAny<object>()), Times.Once);

			#endregion
		}

		[Test]
		public void UpdateUser_WhenAttemptToUpdateAnEmailAddressThatIsBeingUsedForAnotherUser_ThenThrowException()
		{
			#region Arrange

			// Below the original email is admin@email.com but is trying to update to insurance@email.com which is being user for another user
			var userUpdate = new User
			{
				Id = 1,
				Email = "insurance@email.com",
				Psw = null,
				FirstName = "Updated",
				LastName = "Updated",
				RoleId = EnumUserRoleId.Admin,
				Companies = new List<UserCompany>()
			};

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>()
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);

			// NOTE: Don't need initialize the DbSet in context because we are pretending all data is pulled from cache

			#endregion

			#region Act

			#endregion

			#region Assert

			// Verify that an exception is thrown
			Assert.That(() => service.UpdateUser(userUpdate), Throws.Exception.TypeOf<DuplicatedRecordCustomizedException>());
			// Verify that a user_company has NEVER been added to context
			_context.Verify(v => v.user_company.Add(It.IsAny<user_company>()), Times.Never);
			// Verify that a user_company has NEVER been removed from context
			_context.Verify(v => v.user_company.Remove(It.IsAny<user_company>()), Times.Never);
			// Verify the user update has NEVER been commited
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
			// Verify the cache has NEVER been updated
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoUsers, It.IsAny<object>()), Times.Never);

			#endregion
		}

		#endregion

		#region UpdateUserProfile

		[Test]
		public async Task UpdateUserProfile_WhenUserProfileProvided_ThenUpdateUserProfileButNotPsw()
		{
			#region Arrange

			var updatedUserProfile = new UserProfile
			{
				Id = 1,
				Email = "admin@email.com",
				Psw = null,
				FirstName = "Updated",
				LastName = "Updated"
			};
			var updatedUserProfileRequest = new UserService.UpdateUserProfileRequest
			{
				UserProfile = updatedUserProfile
			};

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>()
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// This is a single mocked record from db
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);
			// Initialize DbSet in context
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			#endregion

			#region Act

			var updatedUserProfileResponse = await service.UpdateUserProfile(updatedUserProfileRequest).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify a query executed to find the user
			_context.Verify(v => v.user.FindAsync(It.IsAny<object>()), Times.Once);
			// Verify the user update has been commited
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
			// Verify the cache has been updated
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoUsers), Times.AtLeastOnce);
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoUsers, It.IsAny<object>()), Times.Once);

			// Verify that this entity has NEVER been added to the context
			_userPswHistoryService.Verify(v => v.InsertPasswordHistory(It.IsAny<int>(), It.IsAny<string>()), Times.Never);

			// Verify the user info was updated
			Assert.IsNotNull(updatedUserProfileResponse);
			Assert.IsTrue(string.IsNullOrWhiteSpace(updatedUserProfileResponse.Message));
			Assert.IsTrue(updatedUserProfileResponse.IsOperationSuccesful);
			// Verify the psw was NOT updated/changed
			Assert.IsNotNull(cachedUsers.FirstOrDefault(x => x.Id == updatedUserProfile.Id).Psw);
			Assert.AreEqual(cachedUsers.FirstOrDefault(x => x.Id == updatedUserProfile.Id).Psw, "123");

			#endregion
		}

		[Test]
		public async Task UpdateUserProfile_WhenUserProfileWithPasswordProvided_ThenInsertUserPswHistory()
		{
			#region Arrange

			var updatedUserProfile = new UserProfile
			{
				Id = 1,
				Email = "admin@email.com",
				Psw = "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4",
				FirstName = "Updated",
				LastName = "Updated"
			};
			var updatedUserProfileRequest = new UserService.UpdateUserProfileRequest
			{
				UserProfile = updatedUserProfile
			};

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>()
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			// This is a single mocked record from db
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1
			};

			// When method is called by second time and going forward data is pulled from cache only
			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);
			// When the psw should not be found in history
			_userPswHistoryService.Setup(x => x.ExistsInPasswordHistory(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
			// Initialize DbSet in context
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			this.SetupContext();

			#endregion

			#region Act

			var updatedUserProfileResponse = await service.UpdateUserProfile(updatedUserProfileRequest).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify a query executed to find the user
			_context.Verify(v => v.user.FindAsync(It.IsAny<object>()), Times.Once);
			// Verify the psw is added to history
			_userPswHistoryService.Verify(v => v.InsertPasswordHistory(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
			// Verify the db transaction was commited
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
			// Verify the cache has been updated
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoUsers), Times.AtLeastOnce);
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoUsers, It.IsAny<object>()), Times.Once);

			// Verify the user info was updated
			Assert.IsNotNull(updatedUserProfileResponse);
			Assert.IsTrue(string.IsNullOrWhiteSpace(updatedUserProfileResponse.Message));
			Assert.IsTrue(updatedUserProfileResponse.IsOperationSuccesful);
			// Verify the psw was updated/changed
			Assert.IsNotNull(cachedUsers.FirstOrDefault(x => x.Id == updatedUserProfile.Id).Psw);
			Assert.AreEqual(cachedUsers.FirstOrDefault(x => x.Id == updatedUserProfile.Id).Psw, updatedUserProfile.Psw);

			#endregion
		}

		[Test]
		public async Task UpdateUserProfile_WhenPswExists_ThenDoNotUpdateTheUserInfoNorThePsw()
		{
			#region Arrange

			var updatedUserProfile = new UserProfile
			{
				Id = 1,
				Email = "admin@email.com",
				Psw = "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4",
				FirstName = "Will NOT update",
				LastName = "Will NOT update"
			};
			var updatedUserProfileRequest = new UserService.UpdateUserProfileRequest
			{
				UserProfile = updatedUserProfile
			};

			// These are cached records (notice that it is an object type)
			IEnumerable<User> cachedUsers = new List<User>
			{
				new User
				{
					Id = 1,
					FirstName = "Sys",
					LastName = "Admin",
					Email = "admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.Admin,
					Companies = new List<UserCompany>()
				},
				new User
				{
					Id = 2,
					FirstName = "Insurance",
					LastName = "Read-only",
					Email = "insurance@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.InsuranceReadOnly,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 2,
							CompanyId = 10,
							CompanyTypeId = EnumCompanyTypeId.Insurance,
							UserId = 2
						}
					}
				},
				new User
				{
					Id = 3,
					FirstName = "Store",
					LastName = "Admin",
					Email = "store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 3,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 3
						}
					}
				},
				new User
				{
					Id = 4,
					FirstName = "Store",
					LastName = "Operator",
					Email = "store_operator@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsOperator,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 4,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 4
						}
					}
				},
				new User
				{
					Id = 5,
					FirstName = "Double Store",
					LastName = "Admin",
					Email = "double_store_admin@email.com",
					Psw = "123",
					RoleId = EnumUserRoleId.WsAdmin,
					Companies = new List<UserCompany>
					{
						new UserCompany
						{
							Id = 5,
							CompanyId = 20,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						},
						new UserCompany
						{
							Id = 6,
							CompanyId = 21,
							CompanyTypeId = EnumCompanyTypeId.Store,
							UserId = 5
						}
					}
				}
			};

			_cacheService.Setup(x => x.Exist(It.Is<string>(a => a == _cacheKeyNameForDtoUsers))).Returns(true);
			_cacheService.Setup(x => x.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers)).Returns(cachedUsers);
			_userPswHistoryService.Setup(x => x.ExistsInPasswordHistory(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);
			// Initialize DbSet in context
			this.SetupContext();

			#endregion

			#region Act

			var updatedUserProfileResponse = await service.UpdateUserProfile(updatedUserProfileRequest).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify a query NEVER executed to find the user
			_context.Verify(v => v.user.FindAsync(It.IsAny<object>()), Times.Never);
			// Verify the psw was NOT added to history
			_userPswHistoryService.Verify(v => v.InsertPasswordHistory(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
			// Verify the db transaction was NOT commited
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
			// Verify the cache has NOT been updated
			_cacheService.Verify(v => v.Exist(_cacheKeyNameForDtoUsers), Times.AtLeastOnce);
			_cacheService.Verify(v => v.Set(_cacheKeyNameForDtoUsers, It.IsAny<object>()), Times.Never);

			// Verify the user info was updated
			Assert.IsNotNull(updatedUserProfileResponse);
			Assert.IsFalse(string.IsNullOrWhiteSpace(updatedUserProfileResponse.Message));
			Assert.IsFalse(updatedUserProfileResponse.IsOperationSuccesful);
			// Verify the psw was NOT updated/changed
			Assert.IsNotNull(cachedUsers.FirstOrDefault(x => x.Id == updatedUserProfile.Id).Psw);
			Assert.AreNotEqual(cachedUsers.FirstOrDefault(x => x.Id == updatedUserProfile.Id).Psw, updatedUserProfile.Psw);
			Assert.AreEqual(cachedUsers.FirstOrDefault(x => x.Id == updatedUserProfile.Id).Psw, "123");

			#endregion
		}

		#endregion

		#region ResetPsw

		[Test]
		public async Task ResetPsw_WhenTheAutogeneratedPasswordExistsInHistory_ThenAutogenerateAnotherPassword()
		{
            #region Arrange

            // This is a single mocked record from db
            var user_entity = new user
            {
                user_id = 1,
                user_first_name = "Sys",
                user_last_name = "Admin",
                email = "admin@email.com",
                psw = "123",
                role_id = 1
            };

			_userPswHistoryService.Setup(v => v.ExistsInPasswordHistory(It.IsAny<int>(), It.IsAny<string>(), 1)).ReturnsAsync(true);
			_userPswHistoryService.Setup(v => v.ExistsInPasswordHistory(It.IsAny<int>(), It.IsAny<string>(), 2)).ReturnsAsync(false);

			// Initialize DbSet in context
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);
			
			this.SetupContext();

			#endregion

			#region Act

			var updatedUserProfileResponse = await service.ResetPsw(user_entity.user_id).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify that a news entity was added to the context
			_userPswHistoryService.Verify(v => v.InsertPasswordHistory(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
			// Verify that method makes 2 interactions in the do while loop
			_userPswHistoryService.Verify(v => v.ExistsInPasswordHistory(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));

			// Verify the password has been updated
			Assert.IsNotNull(updatedUserProfileResponse);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(updatedUserProfileResponse.NewPassword));

            #endregion
        }

		[Test]
		public void ResetPsw_WhenTheAutogeneratedPasswordExceedsTheMaxPreexistingVerificationAttempts_ThenThrowInvalidOperationException()
		{
			#region Arrange

			// This is a single mocked record from db
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "123",
				role_id = 1
			};

			_userPswHistoryService.Setup(v => v.ExistsInPasswordHistory(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>())).Throws<InvalidOperationException>();

			// Initialize DbSet in context
			_context.Setup(x => x.user.FindAsync(It.IsAny<object>())).ReturnsAsync(user_entity);

			this.SetupContext();

			#endregion

			#region Act



			#endregion

			#region Assert

			// Verify that get throw exception when exceded invocation number
			Assert.That(() => service.ResetPsw(user_entity.user_id), Throws.Exception.TypeOf<InvalidOperationException>());

			#endregion
		}

		#endregion
	}
}
