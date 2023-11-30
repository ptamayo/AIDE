using Aide.Admin.Domain.Mapping;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Aide.Admin.Services
{
    public interface IUserPswHistoryService
    {
		Task InsertPasswordHistory(int userId, string psw);
		Task<bool> ExistsInPasswordHistory(int userId, string psw, int count = 1);

		Task<user_psw_history> GetUserPswHistory(int userId, string psw);
	}

    public class UserPswHistoryService : IUserPswHistoryService
    {
		#region Properties

		private readonly IServiceProvider _serviceProvider;

		#endregion

		#region Constructor

		public UserPswHistoryService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		#endregion

		#region Methods

		public async Task InsertPasswordHistory(int userId, string psw)
		{
			if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
			if (string.IsNullOrWhiteSpace(psw)) throw new ArgumentNullException(nameof(psw));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var newUserPswHistory = new UserPswHistory
				{
					UserId = userId,
					Psw = psw,
					DateCreated = DateTime.UtcNow
				};

				var userPswHistoryRepository = new EfRepository<user_psw_history>(transientContext);
				var userPswHistoryEntity = UserPswHistoryMap.ToEntity(newUserPswHistory);
				await userPswHistoryRepository.InsertAsync(userPswHistoryEntity).ConfigureAwait(false);
			}
		}

		public async Task<bool> ExistsInPasswordHistory(int userId, string psw, int count = 1)
        {
			if (count >= 10) throw new InvalidOperationException("The max amount of attempts has exceded");
			if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
			if (string.IsNullOrWhiteSpace(psw)) throw new ArgumentNullException(nameof(psw));

			var pswExists = false;
			try
			{
				var result = await GetUserPswHistory(userId, psw).ConfigureAwait(false);
				if (result != null) pswExists = true; // The psw existing in history table
			}
			catch (NonExistingRecordCustomizedException) { } 

			return pswExists;
		}

		public async Task<user_psw_history> GetUserPswHistory(int userId, string psw)
		{
			if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
			if (string.IsNullOrWhiteSpace(psw)) throw new ArgumentNullException(nameof(psw));

			user_psw_history pswHistory = default;
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<user_psw_history>(transientContext);
				var query = from user_psw_history in repository.TableNoTracking
							where user_psw_history.user_id == userId && user_psw_history.psw == psw
							select user_psw_history;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (!entities.Any()) return pswHistory;

				// Get user_psw_history exist
				pswHistory = entities.FirstOrDefault();
			}

			return pswHistory;
		}

        #endregion
    }
}
