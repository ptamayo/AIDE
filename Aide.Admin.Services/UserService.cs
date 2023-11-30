using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Core.Extensions;
using Aide.Core.Interfaces;
using Aide.Admin.Domain.Enumerations;
using Aide.Admin.Domain.Mapping;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Claim = System.Security.Claims.Claim;

namespace Aide.Admin.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsers();
        Task<IPagedResult<User>> GetAllUsers(IPagingSettings pagingSettings, UserService.Filters filters, EnumViewDetail viewDetail);
        Task<User> GetUserById(int UserId);
        Task<User> GetUserByEmail(string email);
        Task<IEnumerable<User>> GetUserListByUserIds(int[] userIds);
        Task<IEnumerable<User>> GetUserListByUserRoleIds(int[] userRoleIds);
        void SetSymmetricSecurityKey(string symmetricSecurityKey);
        Task<UserAuth> Authenticate(string usr, string psw);
        object ReadKeyFromJwtToken(string token, string key);
        Task UserLogout(int userId);
        User BuildUserFromJwtToken(string token);
        Task<UserService.InsertUserResult> InsertUser(User dto);
        Task UpdateUser(User dto);
        Task<UserService.ResetPswResult> ResetPsw(int userId);
        Task<UserService.UpdateUserProfileResponse> UpdateUserProfile(UserService.UpdateUserProfileRequest userProfileRequest);
    }

    public class UserService : IUserService
    {
        #region Properties

        private readonly IJwtSecurityTokenHandlerAdapter _tokenHandler;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICacheService _cacheService;
        private readonly IUserPswHistoryService _userPswHistoryService;
        private const string _cacheKeyNameForDtoUsers = "Dto-List-User";
        private string _symmetricSecurityKey;
        private const int SymmetricSecurityKeyMinLength = 32;
        private SecurityLockConfig _securityLock;

        #endregion

        #region Constructor

        public UserService(IJwtSecurityTokenHandlerAdapter tokenHandler, IServiceProvider serviceProvider, ICacheService cacheService, IUserPswHistoryService userPswHistoryService, SecurityLockConfig securityLockConfig)
        {
            _tokenHandler = tokenHandler ?? throw new ArgumentNullException(nameof(tokenHandler));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _userPswHistoryService = userPswHistoryService ?? throw new ArgumentNullException(nameof(userPswHistoryService));
            _securityLock = securityLockConfig ?? throw new ArgumentNullException(nameof(securityLockConfig));
        }

        #endregion

        #region Methods

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            //Begin Cache
            if (_cacheService.Exist(_cacheKeyNameForDtoUsers))
            {
                return _cacheService.Get<IEnumerable<User>>(_cacheKeyNameForDtoUsers);
            }
            //End Cache

            IEnumerable<User> dtos = new List<User>();
            using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
                var repository = new EfRepository<user>(transientContext);
                var query = from user in repository.TableNoTracking select user;
                var entities = await query.ToListAsync().ConfigureAwait(false);
                if (!entities.Any()) return dtos;
                dtos = UserMap.ToDto(entities);
            }

            // Load metadata
            if (dtos.Any())
            {
                dtos = await LoadMetadata(dtos).ConfigureAwait(false);
            }

            // Order the list of users before caching
            var orderedList = OrderUsers(dtos);

            //Begin Cache
            _cacheService.Set(_cacheKeyNameForDtoUsers, orderedList);
            //End Cache

            return orderedList;
        }

        public async Task<IPagedResult<User>> GetAllUsers(IPagingSettings pagingSettings, UserService.Filters filters, EnumViewDetail viewDetail)
        {
            if (pagingSettings == null) throw new ArgumentNullException(nameof(pagingSettings));
            if (pagingSettings.PageNumber < 1 || pagingSettings.PageSize < 1) throw new ArgumentOutOfRangeException(nameof(pagingSettings));
            if (filters == null) throw new ArgumentNullException(nameof(filters));

            var users = await GetAllUsers().ConfigureAwait(false);
            var query = UserMap.Clone(users).AsQueryable(); // This is very important to prevent changes in the cache

            // Filter by keyword
            if (!string.IsNullOrWhiteSpace(filters.Keywords))
            {
                // Notice the keywords are converted to lowercase. Also there's no need to apply RegexOptions.IgnoreCase.
                // This is because the search will be performed against an EF Model.
                // See InsuranceCompanyService.GetAllClaims(... for an example of a different implementation.
                var keywords = filters.Keywords.EscapeRegexSpecialChars().ToLower().Split(' ');
                var regex = new Regex(string.Join("|", keywords));
                var regexString = regex.ToString();

                // Notice that RegexOptions.IgnoreCase has been applied below.
                // This is becuse the regexp is being applied to a Generic Collection.
                // When regex is applied to a EF object then this is not necessary.
                query = query.Where(x => 1 == 1 &&
                                   (Regex.IsMatch(x.FirstName, regexString, RegexOptions.IgnoreCase) ||
                                    Regex.IsMatch(x.LastName, regexString, RegexOptions.IgnoreCase) ||
                                    Regex.IsMatch(x.Email, regexString, RegexOptions.IgnoreCase)));
            }

            // Filter by company ID
            if (filters.CompanyId.HasValue && filters.CompanyTypeId.HasValue)
            {
                if (filters.CompanyId.Value != 0 && filters.CompanyTypeId.Value != EnumCompanyTypeId.Unknown)
                {
                    query = query.Where(x => x.Companies.Any(y => y.CompanyId == filters.CompanyId.Value && y.CompanyTypeId == filters.CompanyTypeId));
                }
            }

            // Filter by UserRoleIds
            if (filters.UserRoleIds != null && filters.UserRoleIds.Any())
            {
                query = query.Where(x => filters.UserRoleIds.Any(y => y == x.RoleId));
            }

            var page = EfRepository<User>.Paginate(pagingSettings, query);

            //NEED REVISIT
            if (viewDetail == EnumViewDetail.Minimum)
            {
                // The usual strategy is to avoid call the LoadMetadata method but here logic is different
                // because the full catalog of users comes directly from cache so that some children object(s)
                // must be set to null
                page.Results.ToList().ForEach(x => x.Companies = null);
            }

            return page;
        }

        private async Task<IEnumerable<User>> LoadMetadata(IEnumerable<User> dtos)
        {
            if (dtos == null) throw new ArgumentNullException(nameof(dtos));

            if (dtos.Any())
            {
                // User Companies
                IEnumerable<UserCompany> userCompanies = new List<UserCompany>();
                using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
                {
                    var repository = new EfRepository<user_company>(transientContext);
                    var query = from user_company in repository.TableNoTracking select user_company;
                    var entities = await query.ToListAsync().ConfigureAwait(false);
                    if (!entities.Any()) return dtos;
                    userCompanies = UserCompanyMap.ToDto(entities);
                }

                // Attach metadata
                foreach (var dto in dtos)
                {
                    dto.Companies = userCompanies.Where(x => x.UserId == dto.Id).ToList();
                }
            }
            return dtos;
        }

        private IEnumerable<User> OrderUsers(IEnumerable<User> users)
        {
            return users.OrderBy(o1 => o1.FirstName).ThenBy(o2 => o2.LastName);
        }

        public async Task<User> GetUserById(int userId)
        {
            if (userId <= 0) throw new ArgumentException(nameof(userId));

            var users = await this.GetAllUsers().ConfigureAwait(false);
            if (!users.Any()) throw new NonExistingRecordCustomizedException();

            var result = users.Where(x => x.Id == userId).FirstOrDefault();
            if (result == null) throw new NonExistingRecordCustomizedException();

            return result;
        }

        public async Task<User> GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException(nameof(email));

            var users = await this.GetAllUsers().ConfigureAwait(false);
            if (users == null) throw new NonExistingRecordCustomizedException();

            var result = users.FirstOrDefault(x => string.Compare(x.Email.Trim(), email.Trim(), true) == 0);
            if (result == null) throw new NonExistingRecordCustomizedException();

            return result;
        }

        public async Task<IEnumerable<User>> GetUserListByUserIds(int[] userIds)
        {
            if (userIds == null || !userIds.Any()) throw new ArgumentException(nameof(userIds));

            var users = await this.GetAllUsers().ConfigureAwait(false);
            if (users == null || !users.Any()) throw new NonExistingRecordCustomizedException();

            var result = users.Where(x => userIds.Contains(x.Id)).ToList();
            if (result == null || !result.Any()) throw new NonExistingRecordCustomizedException();

            return result;
        }

        public async Task<IEnumerable<User>> GetUserListByUserRoleIds(int[] userRoleIds)
        {
            if (userRoleIds == null || !userRoleIds.Any()) throw new ArgumentException(nameof(userRoleIds));

            var users = await this.GetAllUsers().ConfigureAwait(false);
            if (users == null || !users.Any()) throw new NonExistingRecordCustomizedException();

            var result = users.Where(x => userRoleIds.Contains((int)x.RoleId)).ToList();
            if (result == null || !result.Any()) throw new NonExistingRecordCustomizedException();

            return result.OrderBy(o1 => o1.FirstName).ThenBy(o2 => o2.LastName);
        }

        public void SetSymmetricSecurityKey(string symmetricSecurityKey)
        {
            if (string.IsNullOrWhiteSpace(symmetricSecurityKey)) throw new ArgumentNullException(nameof(symmetricSecurityKey));
            if (symmetricSecurityKey.Length < SymmetricSecurityKeyMinLength) throw new ArgumentOutOfRangeException(nameof(symmetricSecurityKey));
            _symmetricSecurityKey = symmetricSecurityKey;
        }

        public async Task<UserAuth> Authenticate(string usr, string psw)
        {
            if (string.IsNullOrWhiteSpace(usr)) throw new ArgumentNullException(nameof(usr));
            if (string.IsNullOrWhiteSpace(psw)) throw new ArgumentNullException(nameof(psw));

            User user = default;
            try
            {
                // Get user
                user = await GetUserByEmail(usr).ConfigureAwait(false);
            }
            catch (NonExistingRecordCustomizedException)
            {
                return null;
            }
            catch (Exception)
            {
                throw;
            }

            //Validate Password and update security locks in the database
            var userAuth = await ValidateSecurityLock(psw, user).ConfigureAwait(false);

            if (userAuth.IsLoginSuccessful)
            {
                // User companies
                // Notice that only the company Id is added to the token
                // Need revisit because this caused logic repeated in the back-end and front-end in order to determine what's the company type
                var userCompanies = string.Empty;
                if (user.Companies != null && user.Companies.Any())
                {
                    userCompanies = string.Join(",", user.Companies.Select(x => x.CompanyId).ToArray());
                }

                // Generate JWT token
                var key = Encoding.ASCII.GetBytes(_symmetricSecurityKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.GivenName, user.FirstName),
                        new Claim(ClaimTypes.Surname, user.LastName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, Convert.ToString((int)user.RoleId)),
                        new Claim("dateCreated", user.DateCreated.ToString()),
                        new Claim("dateLogout", user.DateLogout.ToString()),
                        new Claim("companies", userCompanies)
                    }),
                    // The expiration of the token is set to midnight
                    Expires = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0).AddDays(1).ToUniversalTime(),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = _tokenHandler.CreateToken(tokenDescriptor);

                // Map User to UserAuth and add JWT token
                userAuth.Token = _tokenHandler.WriteToken(token);
            }

            // Return
            return userAuth;
        }

        private async Task<UserAuth> ValidateSecurityLock(string psw, User dto)
        {
            if (string.IsNullOrWhiteSpace(psw)) throw new ArgumentNullException(nameof(psw));
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (_securityLock == null) throw new ArgumentNullException(nameof(_securityLock));

            var dateTimeNow = DateTime.UtcNow;
            //Validate these are not null nor empty
            var lastLoginAttempt = dto.LastLoginAttempt.HasValue ? dto.LastLoginAttempt.Value : 0;
            var timeLastAttempt = dto.TimeLastAttempt.HasValue ? dto.TimeLastAttempt.Value : dateTimeNow;

            TimeSpan timeElapsedLastAttempt = default;
            if (timeLastAttempt.CompareTo(dateTimeNow) < 0)
                timeElapsedLastAttempt = dateTimeNow - timeLastAttempt;
            else
                timeElapsedLastAttempt = timeLastAttempt - dateTimeNow;

            var isUserLocked = false;
            //Verificar si aun esta dentro de los 15 min de bloqueo
            if (_securityLock.IsEnabled
                && lastLoginAttempt > _securityLock.MaximumAttempts
                && timeElapsedLastAttempt.Ticks < new TimeSpan(0, _securityLock.LockLength, 0).Ticks)
            {
                isUserLocked = true;
            }

            //Si el tiempo es mayor de los 60 min reiniciar variables O si la contrasenia es correcta
            if (timeElapsedLastAttempt.Ticks > new TimeSpan(0, _securityLock.TimeFrame, 0).Ticks)
            {
                lastLoginAttempt = 0;
                timeLastAttempt = dateTimeNow;
                timeElapsedLastAttempt = new TimeSpan(0, 0, 0);
            }

            var message = string.Empty;
            var isLoginSuccessful = false;
            var isPasswordValid = false;
            var saveChange = false;

            if (!isUserLocked)
            {
                if (dto.Psw.Equals(psw))
                {
                    isPasswordValid = true;
                    isLoginSuccessful = true;
                    saveChange = true;
                    lastLoginAttempt = 0;
                    timeLastAttempt = dateTimeNow;
                }
                else
                {
                    isPasswordValid = false;
                    lastLoginAttempt++;
                }

                if (_securityLock.IsEnabled && !isPasswordValid)
                {
                    //Si aun no cumple el maximo establecido guardar en la base de datos
                    if (lastLoginAttempt <= _securityLock.MaximumAttempts + 1)
                    {
                        timeLastAttempt = dateTimeNow;
                        saveChange = true;
                    }
                    else //Si ya paso el tiempo de bloqueo e ingresa mal la contraseña reinicia el contador
                        if (lastLoginAttempt > _securityLock.MaximumAttempts
                            && timeElapsedLastAttempt.Ticks > new TimeSpan(0, _securityLock.LockLength, 0).Ticks)
                    {
                        lastLoginAttempt = 1;
                        timeLastAttempt = dateTimeNow;
                        saveChange = true;
                    }
                }

                if (lastLoginAttempt > 0)
                    message = $"The password is not valid (attempt #{lastLoginAttempt}).";

                if (saveChange)
                    await UpdateLoginAttempt(dto, lastLoginAttempt, timeLastAttempt).ConfigureAwait(false);

                //Verificar si aun esta dentro del maximo establecido
                if (_securityLock.IsEnabled
                    && lastLoginAttempt > _securityLock.MaximumAttempts)
                {
                    isUserLocked = true;
                    isLoginSuccessful = false;
                }
            }

            if (isUserLocked)
                message = $"Your account is locked for the next {_securityLock.LockLength} minutes due to several failed login attempts. Please try again later.";

            var userAuth = new UserAuth
            {
                IsUserLocked = isUserLocked,
                IsLoginSuccessful = isLoginSuccessful,
                Message = message,
            };

            return userAuth;
        }

        private async Task UpdateLoginAttempt(User dto, int lastLoginAttempt, DateTime timeLastAttempt)
        {
            if (dto == null) throw new ArgumentNullException();
            if (dto.Id <= 0) throw new ArgumentOutOfRangeException();
            if (lastLoginAttempt < 0) throw new ArgumentOutOfRangeException();

            // Update user
            using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
                var repository = new EfRepository<user>(transientContext);
                var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
                if (entity == null) throw new NonExistingRecordCustomizedException(nameof(entity));

                entity.last_login_attempt = lastLoginAttempt;
                entity.time_last_attempt = timeLastAttempt;

                await repository.UpdateAsync(entity).ConfigureAwait(false);
            }

            //Begin Update Cache
            if (_cacheService.Exist(_cacheKeyNameForDtoUsers))
            {
                var dtosx = await GetAllUsers().ConfigureAwait(false);
                var dtos = dtosx.ToList();
                var currentDto = dtos.Find(b => b.Id == dto.Id);
                currentDto = UserMap.ToDto(dto, currentDto);
                _cacheService.Set(_cacheKeyNameForDtoUsers, dtos);
            }
            //End Update Cache
        }

        public object ReadKeyFromJwtToken(string token, string key)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            try
            {
                var jwtSecurityToken = _tokenHandler.ReadJwtToken(token);
                return jwtSecurityToken.Payload[key];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UserLogout(int userId)
        {
            if (userId <= 0) throw new ArgumentException(nameof(userId));

            using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
                var repository = new EfRepository<user>(transientContext);
                var entity = await repository.GetByIdAsync(userId).ConfigureAwait(false);
                if (entity == null) throw new NonExistingRecordCustomizedException();

                entity.date_logout = DateTime.UtcNow;
                await repository.UpdateAsync(entity).ConfigureAwait(false);
            }
        }

        public User BuildUserFromJwtToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentNullException(nameof(token));
            token = token.Replace("Bearer", "").Trim();
            var user = new User
            {
                Id = Convert.ToInt32(ReadKeyFromJwtToken(token, "nameid")),
                RoleId = (EnumUserRoleId)Convert.ToInt32(ReadKeyFromJwtToken(token, "role")),
                FirstName = ReadKeyFromJwtToken(token, "given_name").ToString(),
                LastName = ReadKeyFromJwtToken(token, "family_name").ToString(),
                Email = ReadKeyFromJwtToken(token, "email").ToString(),
                DateCreated = Convert.ToDateTime(ReadKeyFromJwtToken(token, "dateCreated")),
                DateLogout = Convert.ToDateTime(ReadKeyFromJwtToken(token, "dateLogout"))
            };

            // Dev Notes: Need revisit. This same logic exists in the client. Need re-think the approach to avoid have this logic repeated in the front-end
            // For further details see auth.service.ts in the client.
            var companyTypeId = EnumCompanyTypeId.Unknown;
            switch (user.RoleId)
            {
                case EnumUserRoleId.InsuranceReadOnly:
                    companyTypeId = EnumCompanyTypeId.Insurance;
                    break;
                case EnumUserRoleId.WsAdmin:
                case EnumUserRoleId.WsOperator:
                    companyTypeId = EnumCompanyTypeId.Store;
                    break;
            }

            var companies = new List<UserCompany>();
            if (!string.IsNullOrWhiteSpace(ReadKeyFromJwtToken(token, "companies").ToString()))
            {
                var companiesString = Convert.ToString(ReadKeyFromJwtToken(token, "companies"));
                foreach (var c in companiesString.Split(","))
                {
                    var userCompany = new UserCompany
                    {
                        UserId = user.Id,
                        CompanyId = Convert.ToInt32(c),
                        CompanyTypeId = companyTypeId
                    };
                    companies.Add(userCompany);
                }
            }
            user.Companies = companies;

            return user;
        }

        /// <summary>
        /// Verify the email address is not being used by an existing user
        /// </summary>
        /// <param name="emailAddress">emailAddress</param>
        /// <returns></returns>
        private async Task<bool> IsEmailUnique(string emailAddress)
        {
            var emailIsUnique = true;
            try
            {
                var existingUser = await GetUserByEmail(emailAddress).ConfigureAwait(false);
                if (existingUser != null) emailIsUnique = false; // The email is being use by an existing user
            }
            catch (NonExistingRecordCustomizedException) { } // This is what we want, the email should not be used for an existing user
            return emailIsUnique;
        }

        private NewPsw GeneratePassword()
        {
            var generator = new Random();
            var temporaryPassword = generator.Next(0, 999999).ToString("D6");
            using (var sha256Hash = SHA256.Create())
            {
                var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(temporaryPassword));
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                var result = new NewPsw
                {
                    Psw = temporaryPassword,
                    Hash = builder.ToString()
                };
                return result;
            }
        }

        public async Task<InsertUserResult> InsertUser(User dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Email)) throw new ArgumentNullException(nameof(dto.Email));

            // Verify the email address is not being used by an existing user
            var emailIsUnique = await IsEmailUnique(dto.Email).ConfigureAwait(false);
            if (!emailIsUnique)
            {
                throw new DuplicatedRecordCustomizedException($"The email address \"{dto.Email}\" is being used for another user already.");
            }

            // Initialize the password
            var newPsw = GeneratePassword();

            dto.Psw = newPsw.Hash;
            // Initialize dates in UTC
            dto.DateCreated = DateTime.UtcNow;
            dto.DateModified = DateTime.UtcNow;
            dto.DateLogout = DateTime.UtcNow;

            // Insert the user
            using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
                var entity = UserMap.ToEntity(dto);
                var repository = new EfRepository<user>(transientContext);
                await repository.InsertAsync(entity).ConfigureAwait(false);
                dto.Id = entity.user_id;

                // Insert the companies
                await InsertUserCompanies(dto.Id, dto.Companies, transientContext).ConfigureAwait(false);

                // Insert the history pasword
                await _userPswHistoryService.InsertPasswordHistory(dto.Id, dto.Psw).ConfigureAwait(false);
            }

            //Begin Cache
            if (_cacheService.Exist(_cacheKeyNameForDtoUsers))
            {
                var dtos = await GetAllUsers().ConfigureAwait(false);
                var list = dtos.ToList();
                list.Add(dto);
                var orderedList = OrderUsers(list);
                _cacheService.Set(_cacheKeyNameForDtoUsers, orderedList);
            }
            //End Cache

            // Prepare and send response to caller
            var response = new InsertUserResult
            {
                TemporaryPassword = newPsw.Psw,
                User = dto
            };
            return response;
        }

        public async Task UpdateUser(User dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.Id <= 0) throw new ArgumentException(nameof(dto.Id));
            if (string.IsNullOrWhiteSpace(dto.Email)) throw new ArgumentNullException(nameof(dto.Email));

            // Verify the email address is not being used by an existing user which is different of the given user
            try
            {
                var preExistingUser = await GetUserByEmail(dto.Email).ConfigureAwait(false);
                if (preExistingUser != null && preExistingUser.Id != dto.Id) throw new DuplicatedRecordCustomizedException($"The email address \"{dto.Email}\" is being used for another user already.");
            }
            catch (NonExistingRecordCustomizedException) { } // If the email is being changed then it should not be used for an existing user
            catch (Exception) // Unhandled exception
            {
                throw;
            }

            // Update the user
            var currentUser = await GetUserById(dto.Id);
            using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
                var repository = new EfRepository<user>(transientContext);
                var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
                if (entity == null) throw new NonExistingRecordCustomizedException(nameof(entity));

                dto.DateModified = DateTime.UtcNow;
                dto.DateCreated = entity.date_created; // This is very important because we don't want to lose the date the user was created
                dto.DateLogout = entity.date_logout; // This is very important because we don't want to lose the date of the latest logout
                if (string.IsNullOrWhiteSpace(dto.Psw))
                {
                    dto.Psw = entity.psw; // This is very important to not lose the password
                }

                entity = UserMap.ToEntity(dto, entity);
                await repository.UpdateAsync(entity).ConfigureAwait(false);

                // Insert companies that were added, if any
                var addedCompanies = dto.Companies.Where(x => !currentUser.Companies.Any(c => c.CompanyId == x.CompanyId)).ToList();
                if (addedCompanies.Any())
                {
                    await InsertUserCompanies(dto.Id, addedCompanies, transientContext).ConfigureAwait(false);
                }

                // Delete companies that were removed, if any
                var removedCompanies = currentUser.Companies.Where(x => !dto.Companies.Any(c => c.CompanyId == x.CompanyId)).ToList();
                if (removedCompanies.Any())
                {
                    var companyIds = removedCompanies.Select(x => x.CompanyId).ToArray();
                    await DeleteUserCompaniesByCompanyIds(dto.Id, companyIds, transientContext).ConfigureAwait(false);
                }
            }

            //Begin Cache
            if (_cacheService.Exist(_cacheKeyNameForDtoUsers))
            {
                var dtos = await GetAllUsers().ConfigureAwait(false);
                var list = dtos.ToList();
                var currentDto = list.Find(li => li.Id == dto.Id);
                currentDto = UserMap.ToDto(dto, currentDto);
                var orderedList = OrderUsers(list);
                _cacheService.Set(_cacheKeyNameForDtoUsers, orderedList);
            }
            //End Cache
        }

        public async Task<ResetPswResult> ResetPsw(int userId)
        {
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));

            NewPsw newPsw = default;
            // Update user
            var dto = new User();
            using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
                var repository = new EfRepository<user>(transientContext);
                var entity = await repository.GetByIdAsync(userId).ConfigureAwait(false);
                if (entity == null) throw new NonExistingRecordCustomizedException(nameof(entity));

                bool isPreexistingPsw = false;
                int count = 1;
                do
                {
                    // Initialize the password
                    newPsw = GeneratePassword();
                    // Verify that the password does not exist in the history table of this user
                    isPreexistingPsw = await _userPswHistoryService.ExistsInPasswordHistory(entity.user_id, newPsw.Hash, count).ConfigureAwait(false);
                    count++;
                }
                while (isPreexistingPsw);

                // Continue set variables
                entity.date_modified = DateTime.UtcNow;
                entity.psw = newPsw.Hash;

                await repository.UpdateAsync(entity).ConfigureAwait(false);
                dto = UserMap.ToDto(entity);

                // Insert the history pasword
                await _userPswHistoryService.InsertPasswordHistory(dto.Id, dto.Psw).ConfigureAwait(false);
            }

            // Update Cache
            //Begin Cache
            if (_cacheService.Exist(_cacheKeyNameForDtoUsers))
            {
                var dtosx = await GetAllUsers().ConfigureAwait(false);
                var dtos = dtosx.ToList();
                var currentDto = dtos.Find(b => b.Id == dto.Id);
                currentDto = UserMap.ToDto(dto, currentDto);
                _cacheService.Set(_cacheKeyNameForDtoUsers, dtos);
            }
            //End Cache

            // Return user details and new psw to the caller
            return new ResetPswResult
            {
                User = dto,
                NewPassword = newPsw.Psw
            };
        }

        private async Task InsertUserCompanies(int userId, IEnumerable<UserCompany> dtos, DbContext transientContext)
        {
            if (userId <= 0) throw new ArgumentException(nameof(userId));
            if (dtos == null) throw new ArgumentNullException(nameof(dtos));
            if (transientContext == null) throw new ArgumentNullException(nameof(transientContext));
            if (dtos.Any())
            {
                var userCompanyRepository = new EfRepository<user_company>(transientContext);
                foreach (var userCompanyDto in dtos)
                {
                    userCompanyDto.UserId = userId;
                    userCompanyDto.DateCreated = DateTime.UtcNow;
                    var userCompanyEntity = UserCompanyMap.ToEntity(userCompanyDto);
                    await userCompanyRepository.InsertAsync(userCompanyEntity).ConfigureAwait(false);
                    userCompanyDto.Id = userCompanyEntity.user_company_id;
                }
            }
        }

        private async Task DeleteUserCompaniesByCompanyIds(int userId, int[] companyIds, DbContext transientContext)
        {
            if (userId <= 0) throw new ArgumentException(nameof(userId));
            if (companyIds == null) throw new ArgumentNullException(nameof(companyIds));
            if (transientContext == null) throw new ArgumentNullException(nameof(transientContext));
            if (companyIds.Any())
            {
                var userCompanyRepository = new EfRepository<user_company>(transientContext);
                var query = from user_company in userCompanyRepository.Table
                            where user_company.user_id == userId && companyIds.Contains(user_company.company_id)
                            select user_company;
                var entities = await query.ToListAsync().ConfigureAwait(false);
                if (entities.Any())
                {
                    await userCompanyRepository.DeleteAsync(entities).ConfigureAwait(false);
                }
            }
        }

        public async Task<UserService.UpdateUserProfileResponse> UpdateUserProfile(UpdateUserProfileRequest userProfileRequest)
        {
            if (userProfileRequest == null || userProfileRequest.UserProfile == null) throw new ArgumentNullException(nameof(userProfileRequest));
            if (userProfileRequest.UserProfile.Id <= 0) throw new ArgumentOutOfRangeException(nameof(userProfileRequest.UserProfile.Id));

            var dto = await GetUserById(userProfileRequest.UserProfile.Id).ConfigureAwait(false);
            if (dto == null) throw new NonExistingRecordCustomizedException();

            dto.Email = userProfileRequest.UserProfile.Email;
            dto.FirstName = userProfileRequest.UserProfile.FirstName;
            dto.LastName = userProfileRequest.UserProfile.LastName;

            var isReadyForUpdate = true;
            var message = string.Empty;
            if (!string.IsNullOrWhiteSpace(userProfileRequest.UserProfile.Psw))
            {
                // Verify that the password does not exist in the history
                var pswExists = await _userPswHistoryService.ExistsInPasswordHistory(dto.Id, userProfileRequest.UserProfile.Psw).ConfigureAwait(false);
                if (pswExists)
                {
                    isReadyForUpdate = false;
                    message = "Cannot update because the password has been used before";
                }
                else
                {
                    // Add the pasword to history
                    await _userPswHistoryService.InsertPasswordHistory(dto.Id, userProfileRequest.UserProfile.Psw).ConfigureAwait(false);
                    // Apply the password update to the user
                    dto.Psw = userProfileRequest.UserProfile.Psw;
                }
            }

            var isOperationSuccessful = false;
            if (isReadyForUpdate)
            {
                await UpdateUser(dto).ConfigureAwait(false);
                isOperationSuccessful = true;
            }

            var updateUserProfileResponse = new UpdateUserProfileResponse
            {
                IsOperationSuccesful = isOperationSuccessful,
                Message = message
            };
            return updateUserProfileResponse;
        }

        #endregion

        #region Local classes

        public class UpdateUserProfileRequest
        {
            public UserProfile UserProfile { get; set; }
        }

        public class UpdateUserProfileResponse
        {
            public bool IsOperationSuccesful { get; set; }
            public string Message { get; set; }
        }

        public class InsertUserResult
        {
            public string TemporaryPassword { get; set; }
            public User User { get; set; }
        }

        public class ResetPswResult
        {
            public string NewPassword { get; set; }
            public User User { get; set; }
        }

        public class NewPsw
        {
            public string Psw { get; set; }
            public string Hash { get; set; }
        }

        public class Filters
        {
            public string Keywords { get; set; }
            public int? CompanyId { get; set; }
            public EnumCompanyTypeId? CompanyTypeId { get; set; }
            public EnumUserRoleId[] UserRoleIds { get; set; }
        }

        public class SecurityLockConfig
        {
            public bool IsEnabled { get; set; }
            public int MaximumAttempts { get; set; }
            public int LockLength { get; set; }
            public int TimeFrame { get; set; }
        }

        #endregion
    }
}
