using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Aide.Core.Interfaces;
using Aide.Notifications.Domain.Enumerations;
using Aide.Notifications.Domain.Mapping;
using Aide.Notifications.Domain.Objects;
using Aide.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aide.Notifications.Services
{
	public interface INotificationService
	{
		Task<NotificationService.NotificationPagedResult> GetAllNotifications(IPagingSettings pagingSettings, NotificationService.Filters filters);
		Task<NotificationService.NotificationPagedResult> GetAllUnreadNotifications(IPagingSettings pagingSettings, NotificationService.Filters filters);
		Task<Notification> InsertNotification(Notification dto);
		Task<Notification> UpdateNotification(Notification dto);
		Task DeleteNotification(int notificationId);
	}

	public class NotificationService : INotificationService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;
		private const int DaysThreshold = 30;

		#endregion

		#region Constructor

		public NotificationService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		#endregion

		#region Methods

		public async Task<NotificationService.NotificationPagedResult> GetAllNotifications(IPagingSettings pagingSettings, NotificationService.Filters filters)
		{
			if (pagingSettings == null) throw new ArgumentNullException(nameof(pagingSettings));
			if (pagingSettings.PageNumber < 1 || pagingSettings.PageSize < 1) throw new ArgumentException(nameof(pagingSettings));

			var userGroups = await LoadUserGroups(filters).ConfigureAwait(false);
			var userGroupsFilter = SqlQueryHelper.BuildWhereOr("target", userGroups);

			IPagedResult<Notification> page;
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				//var whereExpression = "1=1";
				//whereExpression += $" && date_created >= \"{filters.DateCreated}\" && (target == \"{filters.UserEmail}\" || {userGroupsFilter})";
				//if (!string.IsNullOrWhiteSpace(filters.Keywords)) whereExpression += $" && (message_title like \"%{filters.Keywords}%\" || message like \"%{filters.Keywords}%\")";
				var repository = new EfRepository<notification>(transientContext);
				//var p = await repository.GetAllAsync(pagingSettings, whereExpression, "date_created desc").ConfigureAwait(false);
				var query = from notification in repository.TableNoTracking
							where notification.date_created >= filters.DateCreated
								&& (notification.target == filters.UserEmail || userGroups.Contains(notification.target))
							select notification;
				if (!string.IsNullOrWhiteSpace(filters.Keywords))
				{
					// Notice the keywords are NOT converted to lowercase and also RegexOptions.IgnoreCase is being applied.
					// This is because the search will be performed against a collection which is differently of a EF Model.
					// See ClaimService.GetAllClaims(... for an example of a different implementation.
					var keywords = filters.Keywords.EscapeRegexSpecialChars().Split(' ');
					var regex = new Regex(string.Join("|", keywords));
					var regexString = regex.ToString();

					query = from x in query
							where Regex.IsMatch(x.message, regexString, RegexOptions.IgnoreCase)
							select x;
				}

				var p = await EfRepository<notification>.PaginateAsync(pagingSettings, query).ConfigureAwait(false);
				if (!p.Results.Any()) return null;
				page = NotificationMap.ToDto(p);
			}
			var notificationPageResult = new NotificationPagedResult(page);

			// Below gets the count of notifications that are missing the IsRead record for the given user
			notificationPageResult.TotalUnreadNotifications = await GetCountOfUnreadNotifications(filters).ConfigureAwait(false);

			return notificationPageResult;
		}

		public async Task<NotificationService.NotificationPagedResult> GetAllUnreadNotifications(IPagingSettings pagingSettings, NotificationService.Filters filters)
		{
			if (pagingSettings == null) throw new ArgumentNullException(nameof(pagingSettings));
			if (pagingSettings.PageNumber < 1 || pagingSettings.PageSize < 1) throw new ArgumentException(nameof(pagingSettings));
			if (filters == null) throw new ArgumentNullException(nameof(filters));

			var userGroups = await LoadUserGroups(filters).ConfigureAwait(false);
			var userGroupsFilter = SqlQueryHelper.BuildWhereOr("target", userGroups);

			var page = new NotificationPagedResult
			{
				CurrentPage = pagingSettings.PageNumber
			};

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repositoryNotification = new EfRepository<notification>(transientContext);
				var notificationTable = repositoryNotification.TableNoTracking;
				var repositoryNotificationUser = new EfRepository<notification_user>(transientContext);
				var notificationUserTable = repositoryNotificationUser.TableNoTracking;

				var query = from n in notificationTable
							join nu in notificationUserTable.Where(x => x.user_id == filters.UserId).DefaultIfEmpty()
								on n.notification_id equals nu.notification_id into leftJoin
							from ljnu in leftJoin.DefaultIfEmpty()
							where n.date_created >= Convert.ToDateTime(filters.DateCreated)
							   && n.date_created >= DateTime.UtcNow.AddDays(-DaysThreshold)
							   && (n.target == filters.UserEmail || userGroups.Contains(n.target))
							   && ljnu == null
							orderby n.date_created descending
							select new { n, ljnu };

				if (!string.IsNullOrWhiteSpace(filters.Keywords)) query = query.Where(x => x.n.message.Contains(filters.Keywords) || x.n.message.Contains(filters.Keywords));

				var queryResult = await query.Take(pagingSettings.PageSize).Select(x => new Notification
				{
					Id = x.n.notification_id,
					Type = (EnumNotificationTypeId)x.n.notification_type,
					Source = x.n.source,
					Target = x.n.target,
					MessageType = x.n.message_type,
					Message = x.n.message,
					DateCreated = x.n.date_created,
					IsRead = x.ljnu != null
				}).ToListAsync().ConfigureAwait(false);

				page.Results = queryResult;
			}

			page.PageSize = page.Results.Count();
			page.TotalUnreadNotifications = page.Results.Count(x => !x.IsRead);

			return page;
		}

		/// <summary>
		/// Build the list of groups the user is associated.
		/// The default group should be the UserRoleId.
		/// If in the future there's a extension on user groups then here is where they should be loaded.
		/// </summary>
		/// <param name="filters">These are the filters that come from the front-end</param>
		/// <returns></returns>
		private async Task<IEnumerable<string>> LoadUserGroups(NotificationService.Filters filters)
		{
			if (filters == null) throw new ArgumentNullException(nameof(filters));
			var userGroups = new List<string> { filters.UserRoleId.ToString() };
			return await Task.FromResult(userGroups).ConfigureAwait(false);
		}

		private async Task<double> GetCountOfUnreadNotifications(NotificationService.Filters filters)
		{
			if (filters == null) throw new ArgumentNullException(nameof(filters));

			var userGroups = await LoadUserGroups(filters).ConfigureAwait(false);
			var userGroupsFilter = SqlQueryHelper.BuildWhereOr("target", userGroups);

			double totalCount = 0;
			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repositoryNotification = new EfRepository<notification>(transientContext);
				var notificationTable = repositoryNotification.TableNoTracking;
				var repositoryNotificationUser = new EfRepository<notification_user>(transientContext);
				var notificationUserTable = repositoryNotificationUser.TableNoTracking;

				var query = from n in notificationTable
							join nu in notificationUserTable on n.notification_id equals nu.notification_id into leftJoin
							from ljnu in leftJoin.DefaultIfEmpty()
							where n.date_created >= Convert.ToDateTime(filters.DateCreated)
							   && n.date_created >= DateTime.UtcNow.AddDays(-DaysThreshold)
							   && (n.target == filters.UserEmail || userGroups.Contains(n.target))
							   && ljnu == null
							orderby n.date_created descending
							select new { n.notification_id };

				totalCount = query.Count();
			}

			return totalCount;
		}

		public async Task<Notification> InsertNotification(Notification dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));

			dto.DateCreated = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = NotificationMap.ToEntity(dto);
				var repository = new EfRepository<notification>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.notification_id;
			}

			return dto;
		}

		public async Task<Notification> UpdateNotification(Notification dto)
		{
			if (dto == null) throw new ArgumentNullException(nameof(dto));
			if (dto.Id <= 0) throw new ArgumentException(nameof(dto.Id));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<notification>(transientContext);
				var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				dto.DateCreated = entity.date_created;

				entity = NotificationMap.ToEntity(dto, entity);
				await repository.UpdateAsync(entity).ConfigureAwait(false);
			}

			return dto;
		}

		public async Task DeleteNotification(int notificationId)
		{
			if (notificationId <= 0) throw new ArgumentException(nameof(notificationId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<notification>(transientContext);
				var entity = await repository.GetByIdAsync(notificationId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				await repository.DeleteAsync(entity).ConfigureAwait(false);
			}
		}

		#endregion

		#region Local classes

		public class Filters
		{
			public int UserId { get; set; }
			public string UserEmail { get; set; }
			public EnumUserRoleId UserRoleId { get; set; }
			public string Keywords { get; set; }
			public DateTime DateCreated { get; set; }
		}

		public class NotificationPagedResult : IPagedResult<Notification>
		{
			public IEnumerable<Notification> Results { get; set; }
			public int CurrentPage { get; set; }
			public double PageCount { get; set; }
			public int PageSize { get; set; }
			public double RowCount { get; set; }
			public double TotalUnreadNotifications { get; set; }

			public NotificationPagedResult() { }

			public NotificationPagedResult(IPagedResult<Notification> page)
			{
				Results = page.Results;
				CurrentPage = page.CurrentPage;
				PageCount = page.PageCount;
				PageSize = page.PageSize;
				RowCount = page.RowCount;
			}
		}

		#endregion
	}
}
