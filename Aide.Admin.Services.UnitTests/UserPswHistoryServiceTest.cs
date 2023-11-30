using Aide.Admin.Models;
using Moq;
using Moq.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aide.Admin.Services.UnitTests
{
    [TestFixture]
    public class UserPswHistoryServiceTest
    {
        private IUserPswHistoryService service;
		private Mock<IServiceProvider> _serviceProvider;
		private Mock<AideDbContext> _context;

		[SetUp]
		public void Setup()
		{
			_serviceProvider = new Mock<IServiceProvider>();
			_context = new Mock<AideDbContext>();
			service = new UserPswHistoryService(_serviceProvider.Object);
		}

		/// <summary>
		/// Setup the context.
		/// Need call this in scenarios that will query the database.
		/// IMPORTANT: The entity initialization must happen before calling this.
		/// </summary>
		private void SetupContext()
		{
			// For passing the DbSet from context to EfRepository
			_context.Setup(x => x.Set<user_psw_history>()).Returns(_context.Object.user_psw_history);
			// Initialize context scopee
			_serviceProvider.Setup(x => x.GetService(typeof(AideDbContext))).Returns(_context.Object);
		}

		#region InsertPasswordHistory

		[Test]
		public async Task InsertPasswordHistory_WhenUpdatedPsw_ThenInsertInTheHistory()
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

			// Setup auto increment on user_psw_history.user_psw_history_id
			_context.Setup(x => x.user_psw_history.Add(It.IsAny<user_psw_history>())).Callback<user_psw_history>((new_entity) =>
			{
				var nextUserPswHistoryId = 1;
				new_entity.user_psw_history_id = nextUserPswHistoryId;
				new_entity.user_id = 1;
				new_entity.psw = "f321baeab269563d10b9f3a6150561d856f886e1abd2037b215d5daca8bb8650";
				new_entity.date_created = DateTime.UtcNow;
			});

			this.SetupContext();

			#endregion

			#region Act

			await service.InsertPasswordHistory(user_entity.user_id, user_entity.psw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify that a user_company has NEVER been added to context
			_context.Verify(v => v.user_psw_history.Add(It.IsAny<user_psw_history>()), Times.Once);
			// Verify the db transaction was commited: 2 time for user and user_psw_history
			_context.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

			#endregion
		}

		#endregion

		#region ExistsInPasswordHistory

		[Test]
		public async Task ExistsInPasswordHistory_WhenPswExists_ThenReturnTrue()
		{
			#region Arrange

			// This is a single mocked record from db
			var user_entity = new user
			{
				user_id = 1,
				user_first_name = "Sys",
				user_last_name = "Admin",
				email = "admin@email.com",
				psw = "e3ac7ff518f51438a3870813cc66c0a2361905ec21a64b0c835ec0cde0920167",
				role_id = 1
			};

			// These are the mocked records from db
			var user_psw_history_entities = new List<user_psw_history>
			{
				new user_psw_history
				{
					user_psw_history_id = 1,
					user_id = 1,
					psw = "e3ac7ff518f51438a3870813cc66c0a2361905ec21a64b0c835ec0cde0920167",
					date_created = DateTime.UtcNow
				},
				new user_psw_history
				{
					user_psw_history_id = 2,
					user_id = 1,
					psw = "09a1ab453a9baf445b3e076060734cbfdbdfc3c3f7032f5ff8b970ec36c08b0f",
					date_created = DateTime.UtcNow
				},
				new user_psw_history
				{
					user_psw_history_id = 3,
					user_id = 2,
					psw = "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4",
					date_created = DateTime.UtcNow
				}
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user_psw_history).ReturnsDbSet(user_psw_history_entities);

			this.SetupContext();

			#endregion

			#region Act

			var result = await service.ExistsInPasswordHistory(user_entity.user_id, user_entity.psw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify that password exists
			Assert.IsTrue(result);

			#endregion
		}

		[Test]
		public async Task ExistsInPasswordHistory_WhenPswIsNotExisting_ThenReturnFalse()
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

			// These are the mocked records from db
			var user_psw_history_entities = new List<user_psw_history>
			{
				new user_psw_history
				{
					user_psw_history_id = 1,
					user_id = 1,
					psw = "e3ac7ff518f51438a3870813cc66c0a2361905ec21a64b0c835ec0cde0920167",
					date_created = DateTime.UtcNow
				},
				new user_psw_history
				{
					user_psw_history_id = 2,
					user_id = 1,
					psw = "09a1ab453a9baf445b3e076060734cbfdbdfc3c3f7032f5ff8b970ec36c08b0f",
					date_created = DateTime.UtcNow
				},
				new user_psw_history
				{
					user_psw_history_id = 3,
					user_id = 2,
					psw = "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4",
					date_created = DateTime.UtcNow
				}
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user_psw_history).ReturnsDbSet(user_psw_history_entities);

			this.SetupContext();

            #endregion

            #region Act

            var result = await service.ExistsInPasswordHistory(user_entity.user_id, user_entity.psw).ConfigureAwait(false);

			#endregion

			#region Assert

			// Verify that Not Exists In Password History
			Assert.IsFalse(result);

			#endregion
}
		
		[Test]
		public void ExistsInPasswordHistory_WhenExceededInvocationNumber_ThenThrowInvalidOperationException()
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

			// These are the mocked records from db
			var user_psw_history_entities = new List<user_psw_history>
			{
				new user_psw_history
				{
					user_psw_history_id = 1,
					user_id = 1,
					psw = "e3ac7ff518f51438a3870813cc66c0a2361905ec21a64b0c835ec0cde0920167",
					date_created = DateTime.UtcNow
				},
				new user_psw_history
				{
					user_psw_history_id = 2,
					user_id = 1,
					psw = "09a1ab453a9baf445b3e076060734cbfdbdfc3c3f7032f5ff8b970ec36c08b0f",
					date_created = DateTime.UtcNow
				},
				new user_psw_history
				{
					user_psw_history_id = 3,
					user_id = 2,
					psw = "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4",
					date_created = DateTime.UtcNow
				}
			};

			// Initialize DbSet in context
			_context.Setup(x => x.user_psw_history).ReturnsDbSet(user_psw_history_entities);

			this.SetupContext();

			#endregion

			#region Act

			#endregion

			#region Assert

			// Verify that get throw exception when exceded invocation number
			Assert.That(() => service.ExistsInPasswordHistory(user_entity.user_id, user_entity.psw, 10), Throws.Exception.TypeOf<InvalidOperationException>());

			#endregion
		}

		#endregion
	}
}
