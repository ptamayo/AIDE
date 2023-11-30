using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Notifications.Domain.Enumerations;
using Aide.Notifications.Domain.Objects;
using Aide.Notifications.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aide.Notifications.WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class NotificationUserController : ControllerBase
    {
        private readonly ILogger<NotificationUserController> _logger;
        private readonly INotificationService _notificationService;
        private readonly INotificationUserService _notificationUserService;

        public NotificationUserController(ILogger<NotificationUserController> logger, INotificationService notificationService, INotificationUserService notificationUserService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _notificationUserService = notificationUserService ?? throw new ArgumentNullException(nameof(notificationUserService));
        }

        [HttpPost]
        public async Task<IActionResult> GetUserNotifications(PagingAndFiltering pagingAndFiltering)
        {
            if (pagingAndFiltering == null) return BadRequest();
            if (pagingAndFiltering.PageNumber < 1 || pagingAndFiltering.PageSize < 1) return BadRequest();

            var pagingSettings = pagingAndFiltering.ToPagingSettings();
            var filters = pagingAndFiltering.ToFilters();

            try
            {
                if (pagingAndFiltering.UnreadNotificationsOnly)
                {
                    var pagex = await _notificationService.GetAllUnreadNotifications(pagingSettings, filters);
                    return Ok(pagex);
                }
                var page = await _notificationService.GetAllNotifications(pagingSettings, filters);
                return Ok(page);
            }
            catch (NonExistingRecordCustomizedException)
            {
                //return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the notifications for user {filters.UserEmail}");
                throw;
            }
        }

        [HttpPost("read")]
        public async Task<IActionResult> PostReadUserNotification(ReadNotificationRequest request)
        {
            if (request == null) return BadRequest();
            try
            {
                var notificationUser = request.ToNotificationUser();
                var result = await _notificationUserService.InsertNotificationUser(notificationUser.ToList());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't mark notification(s) as read for user {request.UserId}");
                throw;
            }
        }

        #region Local classes

        public class PagingAndFiltering
        {
            public int PageSize { get; set; }
            public int PageNumber { get; set; }
            public int UserId { get; set; }
            public string UserEmail { get; set; }
            public EnumUserRoleId UserRoleId { get; set; }
            public string DateCreated { get; set; }
            public string DateLogout { get; set; }
            public string Keywords { get; set; }
            public bool UnreadNotificationsOnly { get; set; }
        }

        public class ReadNotificationRequest
        {
            public int UserId { get; set; }
            public int[] NotificationId { get; set; }
        }

        #endregion
    }

    #region Extension methods

    public static class PagingAndFilteringExtensions
    {
        public static PagingSettings ToPagingSettings(this NotificationUserController.PagingAndFiltering pagingAndFiltering)
        {
            return new PagingSettings
            {
                PageNumber = pagingAndFiltering.PageNumber,
                PageSize = pagingAndFiltering.PageSize
            };
        }

        public static NotificationService.Filters ToFilters(this NotificationUserController.PagingAndFiltering pagingAndFiltering)
        {
            return new NotificationService.Filters
            {
                UserId = pagingAndFiltering.UserId,
                UserEmail = pagingAndFiltering.UserEmail,
                UserRoleId = pagingAndFiltering.UserRoleId,
                DateCreated = Convert.ToDateTime(pagingAndFiltering.DateCreated),
                Keywords = pagingAndFiltering.Keywords
            };
        }
    }

    public static class ReadNotificationRequestExtensions
    {
        public static IEnumerable<NotificationUser> ToNotificationUser(this NotificationUserController.ReadNotificationRequest request)
        {
            return request.NotificationId.Select(notificationId => new NotificationUser
            {
                NotificationId = notificationId,
                UserId = request.UserId
            });
        }
    }

    #endregion
}