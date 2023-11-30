using System;
using System.Threading.Tasks;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Services;
using Aide.Admin.WebApi.Adapters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aide.Admin.WebApi.Controllers
{
	[Authorize]
	[Route("[controller]")]
	[ApiController]
	public class UserController : ControllerBase
	{
		private readonly ILogger<UserController> _logger;
		private readonly IUserService _userService;
		private readonly IMessageBrokerAdapter _messageBrokerAdapter;
		private readonly AppSettings _appSettings;

		public UserController(ILogger<UserController> logger, IUserService userService, IMessageBrokerAdapter messageBrokerAdapter, AppSettings appSettings)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_userService = userService ?? throw new ArgumentNullException(nameof(userService));
			_messageBrokerAdapter = messageBrokerAdapter ?? throw new ArgumentNullException(nameof(messageBrokerAdapter));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_userService.SetSymmetricSecurityKey(_appSettings.AuthenticationConfig.SymmetricSecurityKey);
		}

		[HttpPost]
		public async Task<IActionResult> Insert(User user)
		{
			if (user == null) return BadRequest();
			if (string.IsNullOrEmpty(user.Email)) return BadRequest();
			user.Email = user.Email.Replace(" ", "");
			try
			{
				var result = await _userService.InsertUser(user);

				// Send welcome email
				await _messageBrokerAdapter.SendNewUserMessage(result.User.FirstName, result.User.Email, result.TemporaryPassword);

				return Ok(result.User);
			}
			catch (DuplicatedRecordCustomizedException ex)
			{
				_logger.LogError(ex, "Couldn't create a new user due an email dup.");
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Couldn't create a new user due an unhandled exception");
				throw;
			}
		}

		[HttpPut]
		public async Task<IActionResult> Update(User user)
		{
			if (user == null) return BadRequest();
			if (user.Id <= 0) return BadRequest();
			if (string.IsNullOrEmpty(user.Email)) return BadRequest();
			user.Email = user.Email.Replace(" ", "");
			try
			{
				await _userService.UpdateUser(user);
				return Ok(user);
			}
			catch (DuplicatedRecordCustomizedException ex)
			{
				_logger.LogError(ex, "Couldn't update the user due an email dup.");
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Couldn't update the user due an unhandled exception");
				throw;
			}
		}

		[HttpPost("reset")]
		public async Task<IActionResult> ResetPsw(UserPswResetRequest request)
		{
			if (request == null) return BadRequest();
			if (request.UserId <= 0) return BadRequest();
			try
			{
				var result = await _userService.ResetPsw(request.UserId);

				// Send reset user password email
				await _messageBrokerAdapter.SendResetUserPswMessage(result.User.FirstName, result.User.Email, result.NewPassword);

				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Couldn't reset the password of the user due an unhandled exception");
				throw;
			}
		}

		[AllowAnonymous]
		[HttpPost("authenticate")]
		public async Task<IActionResult> Authenticate([FromBody] AuthenticateModel model)
		{
			if (model == null) return BadRequest();
			try
			{
				var userAuth = await _userService.Authenticate(model.Email, model.Password);

				if (userAuth == null) return BadRequest(new { message = "Username or password is incorrect" });

				return Ok(userAuth);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't authenticate user {model.Email}");
				throw;
			}
		}

		[AllowAnonymous]
		[HttpPost("logout")]
		public async Task<IActionResult> Logout(LogoutRequest request)
		{
			if (request == null) return BadRequest();
			if (request.UserId <= 0) return BadRequest();
			await _userService.UserLogout(request.UserId);
			return Ok();
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			if (id <= 0) return BadRequest();
			try
			{
				var result = await _userService.GetUserById(id);
				return Ok(result);
			}
			catch (NonExistingRecordCustomizedException)
			{
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't get a User by ID {id}.");
				throw;
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetByEmail([FromQuery] string email)
		{
			if (string.IsNullOrWhiteSpace(email)) return BadRequest();
			try
			{
				if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
				email = email.Trim().DecodeUTF8();
				var result = await _userService.GetUserByEmail(email);
				return Ok(result);
			}
			catch (NonExistingRecordCustomizedException)
			{
				return NoContent();
			}
			catch (ArgumentNullException ex)
			{
				_logger.LogError(ex, "You haven't provided an email address.");
				return BadRequest("You haven't provided an email address.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't get a User by Email {email}.");
				throw;
			}
		}

		[HttpPost("list")]
		public async Task<IActionResult> GetList(PagingAndFiltering pagingAndFiltering)
		{
			if (pagingAndFiltering == null) return BadRequest();
			if (pagingAndFiltering.PageNumber < 1 || pagingAndFiltering.PageSize < 1) return BadRequest();

			try
			{
				var filters = pagingAndFiltering.ToFilters();
				var pagingSettings = pagingAndFiltering.ToPagingSettings();
				var page = await _userService.GetAllUsers(pagingSettings, filters, EnumViewDetail.Minimum);
				return Ok(page);
			}
			catch (NonExistingRecordCustomizedException)
			{
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Couldn't get the list of users.");
				throw;
			}
		}

		/// <summary>
		/// This is for INTERNAL/PRIVATE use only and SHOULD NOT be exposed to the PUBLIC in the API GATEWAY
		/// </summary>
		/// <param name="userIds">userIds</param>
		/// <returns></returns>
		[HttpPost("listByIds")]
		public async Task<IActionResult> GetListByUserIds(int[] userIds)
		{
			if (userIds == null) return BadRequest();
			if (userIds.Length == 0) return BadRequest();

			try
			{
				var result = await _userService.GetUserListByUserIds(userIds);
				return Ok(result);
			}
			catch (NonExistingRecordCustomizedException)
			{
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed trying to get the list of users.");
				throw;
			}
		}

        [HttpPost("listByRoleIds")]
        public async Task<IActionResult> GetListByUserRoleIds(int[] userRoleIds)
        {
            if (userRoleIds == null) return BadRequest();
            if (userRoleIds.Length == 0) return BadRequest();

            try
            {
                var result = await _userService.GetUserListByUserRoleIds(userRoleIds);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed trying to get the list of users.");
                throw;
            }
        }

        [HttpPut("profile")]
		public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfile userProfile)
		{
			if (userProfile == null) return BadRequest();
			try
			{
				var request = userProfile.ToUpdateUserProfileRequest();
				var result = await _userService.UpdateUserProfile(request);
				return Ok(result);
			}
			catch (NonExistingRecordCustomizedException)
			{
				return NoContent();
			}
			catch (Exception)
			{
				throw;
			}
		}

		#region Local classes

		public class AuthenticateModel
		{
			public string Email { get; set; }
			public string Password { get; set; }
		}

		public class UserPswResetRequest
		{
			public int UserId { get; set; }
		}

		public class LogoutRequest
		{
			public int UserId { get; set; }
		}

		public class PagingAndFiltering
		{
			public int PageSize { get; set; }
			public int PageNumber { get; set; }
			public string Keywords { get; set; }
			public int? CompanyId { get; set; }
			public EnumCompanyTypeId? CompanyTypeId { get; set; }
			public EnumUserRoleId[] UserRoleIds { get; set; }
		}

		#endregion
	}

	#region Extension methods

	public static class UserControllerPagingAndFilteringExtensions
	{
		public static PagingSettings ToPagingSettings(this UserController.PagingAndFiltering pagingAndFiltering)
		{
			return new PagingSettings
			{
				PageNumber = pagingAndFiltering.PageNumber,
				PageSize = pagingAndFiltering.PageSize
			};
		}

		public static UserService.Filters ToFilters(this UserController.PagingAndFiltering pagingAndFiltering)
		{
			return new UserService.Filters
			{
				Keywords = pagingAndFiltering.Keywords.DecodeUTF8().CleanDoubleWhiteSpaces(),
				CompanyId = pagingAndFiltering.CompanyId,
				CompanyTypeId = pagingAndFiltering.CompanyTypeId,
				UserRoleIds = pagingAndFiltering.UserRoleIds
			};
		}

		public static UserService.UpdateUserProfileRequest ToUpdateUserProfileRequest(this UserProfile userProfile)
        {
			return new UserService.UpdateUserProfileRequest
			{
				UserProfile = userProfile
			};
        }
	}

	#endregion
}