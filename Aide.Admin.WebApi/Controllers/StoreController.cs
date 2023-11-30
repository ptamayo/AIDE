using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aide.Admin.WebApi.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IUserService _userService;
        private readonly IStoreService _storeService;

        public StoreController(ILogger<StoreController> logger, IUserService userService, IStoreService storeService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _storeService = storeService ?? throw new ArgumentNullException(nameof(storeService));
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            User user;
            try
            {
                var token = Request.GetAuthorizationHeader();
                user = _userService.BuildUserFromJwtToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get the authorization header (jwt token).");
                throw;
            }

            try
            {
                var result = await _storeService.GetAllStores();
                if (user.RoleId == EnumUserRoleId.WsAdmin || user.RoleId == EnumUserRoleId.WsOperator)
                {
                    var userStores = user.Companies
                        .Where(x => x.CompanyTypeId == EnumCompanyTypeId.Store)
                        .Select(x => x.CompanyId);
                    result = result.Where(x => userStores.Contains(x.Id));
                }
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get the list of stores.");
                throw;
            }
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetList(PagingAndFiltering pagingAndFiltering)
        {
            if (pagingAndFiltering == null) return BadRequest();
            if (pagingAndFiltering.PageNumber < 1 || pagingAndFiltering.PageSize < 1) return BadRequest();

            var pagingSettings = pagingAndFiltering.ToPagingSettings();
            var filters = pagingAndFiltering.ToFilters();

            try
            {
                var page = await _storeService.GetAllStores(pagingSettings, filters);
                return Ok(page);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get the list of stores.");
                throw;
            }
        }

        /// <summary>
		/// This is for INTERNAL/PRIVATE use only and SHOULD NOT be exposed to the PUBLIC in the API GATEWAY
		/// </summary>
		/// <param name="userIds">userIds</param>
		/// <returns></returns>
		[HttpPost("listByIds")]
        public async Task<IActionResult> GetListByStoreIds(int[] storeIds)
        {
            if (storeIds == null) return BadRequest();
            if (storeIds.Length == 0) return BadRequest();

            try
            {
                var result = await _storeService.GetStoreListByStoreIds(storeIds);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get the list of stores.");
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest();
            try
            {
                var result = await _storeService.GetStoreById(id);
                return Ok(result);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't get the store ID {id}.");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Insert(Store store)
        {
            if (store == null) return BadRequest();
            try
            {
                await _storeService.InsertStore(store);
                return Ok(store);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't create the store.");
                throw;
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Store store)
        {
            if (store == null) return BadRequest();
            try
            {
                await _storeService.UpdateStore(store);
                return Ok(store);
            }
            catch (NonExistingRecordCustomizedException ex)
            {
                _logger.LogWarning(ex, $"Cannot update because the store ID {store.Id} has not been found.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Couldn't update the store ID {store.Id}.");
                throw;
            }
        }

        [HttpPost("{companyId}/users")]
        public async Task<IActionResult> GetUserListByCompanyId([FromRoute] int companyId, [FromBody] UserController.PagingAndFiltering pagingAndFiltering)
        {
			if (companyId <= 0) throw new ArgumentNullException(nameof(companyId));
            if (pagingAndFiltering == null) return BadRequest();
            if (pagingAndFiltering.PageNumber < 1 || pagingAndFiltering.PageSize < 1) return BadRequest();

            try
			{
                var filters = pagingAndFiltering.ToFilters();
                var pagingSettings = pagingAndFiltering.ToPagingSettings();
                var page = await _userService.GetAllUsers(pagingSettings, filters, EnumViewDetail.Minimum);
                return Ok(page);
            }
			catch (Exception ex)
			{
                _logger.LogError(ex, $"Couldn't get the list of users by Company ID {companyId} and Company Type {EnumCompanyTypeId.Store}");
				throw;
			}
		}

        #region Local classes

        public class PagingAndFiltering
        {
            public int PageSize { get; set; }
            public int PageNumber { get; set; }
            public string Keywords { get; set; }
        }

        #endregion
    }

    #region Extension methods

    public static class StoreControllerPagingAndFilteringExtensions
    {
        public static PagingSettings ToPagingSettings(this StoreController.PagingAndFiltering pagingAndFiltering)
        {
            return new PagingSettings
            {
                PageNumber = pagingAndFiltering.PageNumber,
                PageSize = pagingAndFiltering.PageSize
            };
        }

        public static StoreService.Filters ToFilters(this StoreController.PagingAndFiltering pagingAndFiltering)
        {
            return new StoreService.Filters
            {
                Keywords = pagingAndFiltering.Keywords.DecodeUTF8().CleanDoubleWhiteSpaces()
            };
        }
    }

    #endregion
}