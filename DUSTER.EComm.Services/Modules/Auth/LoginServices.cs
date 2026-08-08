using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.CacheMemory;
using DUSTER.EComm.Services.CommonServices;
using DUSTER.EComm.Services.Modules.Auth.Models;
using DUSTER.EComm.Services.Modules.RightsMasters.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;

namespace DUSTER.EComm.Services.Modules.Auth
{
    public class LoginServices : ILoginServices
    {
        private readonly IEIPLRepository<UsersModel> _userRepo;
        private readonly IPostgresDistributedCache _cache;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEIPLRepository<UserToken> _userTokenRepo;
        private readonly IConfiguration _config;

        public LoginServices(IEIPLRepository<UsersModel> userRepo, IPostgresDistributedCache cache, JwtTokenService jwtTokenService, IHttpContextAccessor httpContextAccessor, IEIPLRepository<UserToken> userTokenRepo,
            IConfiguration config)
        {
            _userRepo = userRepo;
            _cache = cache;
            _jwtTokenService = jwtTokenService;
            _httpContextAccessor = httpContextAccessor;
            _userTokenRepo = userTokenRepo;
            _config = config;
        }

        public async Task<UserToken> PortalLogin(string userName, string password)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("username", userName);

                var usersModel = (await _userRepo.QueryAsync<UsersModel>("select * from tbl_users where user_name=@username or email_id=@username", param)).FirstOrDefault();

                if (usersModel != null)
                {
                    if (usersModel.is_block == true)
                        return null;

                    if (usersModel.is_delete == true)
                        return null;

                    bool passwordMatch = PasswordUtility.VerifyPassword(password, usersModel.password_hash, usersModel.password_salt);

                    if (!passwordMatch)
                        return null;

                    var roleParam = new DynamicParameters();
                    roleParam.Add("user_code", usersModel.user_code);

                    var userRoles = (await _userRepo.QueryAsync<UserRoleMapping>("select role_code from tbl_user_role_mapping where user_code=@user_code", roleParam)).ToList();

                    if (userRoles == null || userRoles.Count == 0)
                        return null;

                    var actionParam = new DynamicParameters();
                    int[] roles = userRoles.Select(x => x.role_code).ToArray();
                    actionParam.Add("role_code", roles);

                    var actions = (await _userRepo.QueryAsync<ActionModel>(@"select distinct a.* from tbl_actions as a
                                                                            inner join tbl_permission_action_mapping as b on b.action_code=a.action_code
                                                                            inner join tbl_role_permission_mapping as c on b.permission_code=c.permission_code
                                                                            where c.role_code = ANY(@role_code) ", actionParam)).ToList();

                    if (actions == null)
                        return null;

                    CurrentUser currentUser = new CurrentUser()
                    {
                        user_code = usersModel.user_code,
                        user_name = usersModel.user_name,
                        mobile_no = usersModel.mobile_no,
                        email = usersModel.email_id,
                        user_type = usersModel.user_type,
                    };

                    if (currentUser != null)
                    {
                        var token = _jwtTokenService.GenerateToken(currentUser);
                        var refreshToken = _jwtTokenService.GenerateRefreshToken();

                        UserToken userToken = new UserToken()
                        {
                            user_code = currentUser.user_code,
                            access_token = token,
                            refresh_token = refreshToken,
                            access_token_expiry = DateTime.Now.AddDays(5),
                            refresh_token_expiry = DateTime.Now.AddDays(7),
                            device_info = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString(),
                            ip_address = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0",
                            is_active = true,
                            device_type = "web browser",
                            user_type = currentUser.user_type
                        };

                        var result = await _userTokenRepo.InsertAsync(userToken);

                        await SaveUserInCache(usersModel, token);

                        return userToken;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<UserToken> RefreshToken()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                string? refreshToken = httpContext?.Request?.Cookies["refreshToken"];

                if (string.IsNullOrWhiteSpace(refreshToken))
                    throw new UnauthorizedAccessException("Refresh token is missing or invalid.");

                // --- 1️ Fetch current token record from DB ---
                var param = new DynamicParameters();
                param.Add("refreshToken", refreshToken);
                var userToken = await _userTokenRepo.QueryAsync<UserToken>("SELECT * FROM tbl_user_token WHERE refresh_token = @refreshToken AND is_active = true", param);

                if (!userToken.Any())
                    throw new UnauthorizedAccessException("Refresh token is missing or invalid.");

                UserToken oldUserToken = userToken.First();

                if (oldUserToken.refresh_token_expiry <= DateTime.UtcNow)
                    throw new UnauthorizedAccessException("Refresh token expired.");

                // --- 2️ Deserialize user info from stored token or lookup ---
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(oldUserToken.access_token);
                var currentUserJson = jwt.Claims.FirstOrDefault(c => c.Type == "currentUser")?.Value;

                if (string.IsNullOrEmpty(currentUserJson))
                    throw new UnauthorizedAccessException("User payload missing in token.");

                var currentUser = JsonConvert.DeserializeObject<CurrentUser>(currentUserJson);

                // --- 3️ Generate new tokens ---
                string newAccessToken = _jwtTokenService.GenerateToken(currentUser);
                string newRefreshToken = _jwtTokenService.GenerateRefreshToken();

                // --- 4️ Invalidate old record & insert new one ---
                oldUserToken.is_active = false;
                await _userTokenRepo.UpdateAsync(oldUserToken);

                var newUserToken = new UserToken
                {
                    user_code = currentUser.user_code,
                    access_token = newAccessToken,
                    refresh_token = newRefreshToken,
                    access_token_expiry = DateTime.UtcNow.AddMinutes(5),
                    refresh_token_expiry = DateTime.UtcNow.AddDays(7),
                    device_info = httpContext.Request.Headers["User-Agent"].ToString(),
                    ip_address = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    is_active = true
                };

                await _userTokenRepo.InsertAsync(newUserToken);

                // --- 5️  Update HttpOnly refresh cookie ---
                httpContext.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = newUserToken.refresh_token_expiry,
                    SameSite = SameSiteMode.Strict,
                    IsEssential = true
                });

                // --- 6️ Return new short‑lived access token & expiry ---
                return newUserToken;
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception) { throw; }
        }

        public async Task<IActionResult> MobilesLogin(LoginModel model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Username or Password Incorrect.", HttpStatusCode.InternalServerError);

                var param = new DynamicParameters();
                param.Add("username", model.user_name);

                var user = (await _userRepo.QueryAsync<UsersModel>("select * from tbl_users where (user_name=@username or email_id=@username)", param)).FirstOrDefault();

                if (user == null)
                    return ResponseEntity<object>.Error(null, "Username or Password Incorrect.", HttpStatusCode.InternalServerError);

                if (user.is_block == true)
                    return ResponseEntity<object>.Error(null, "Your Account is Blocked. Please contact to support.", HttpStatusCode.InternalServerError);

                if (user.is_delete == true)
                    return ResponseEntity<object>.Error(null, "User Not Found. Please contact to support.", HttpStatusCode.InternalServerError);

                bool passwordMatch = PasswordUtility.VerifyPassword(model.password, user.password_hash, user.password_salt);

                if (!passwordMatch)
                    return ResponseEntity<object>.Error(null, "Username or Passowrd Incorrect", HttpStatusCode.InternalServerError);

                CurrentUser currentUser = new CurrentUser()
                {
                    user_code = user.user_code,
                    user_name = user.user_name,
                    user_type = user.user_type,
                    mobile_no = user.mobile_no
                };

                if (currentUser != null)
                {
                    var token = _jwtTokenService.GenerateToken(currentUser);
                    var refreshToken = _jwtTokenService.GenerateRefreshToken();

                    UserToken userToken = new UserToken()
                    {
                        user_code = currentUser.user_code,
                        access_token = token,
                        refresh_token = refreshToken,
                        access_token_expiry = DateTime.Now.AddDays(5),
                        refresh_token_expiry = DateTime.Now.AddDays(7),
                        device_info = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString(),
                        ip_address = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0",
                        is_active = true,
                        device_id = model.device_id,
                        device_type = model.device_type
                    };

                    var result = await _userTokenRepo.InsertAsync(userToken);

                    //set database cache
                    //await _cache.SetObjectAsync<CurrentUser>(token, currentUser, new TimeSpan(240, 0, 0));
                    await SaveUserInCache(user, token);

                    return ResponseEntity<object>.Success(new { token = token, UserName = user.user_name, UserType = user.user_type, UserCode = user.user_code, MobileNo = user.mobile_no }, "Login Successfully.");
                }

                return ResponseEntity<object>.Error(null, "Login Error.", HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> SaveUserInCache(UsersModel user, string token)
        {
            try
            {
                user.password = null;
                user.password_hash = null;
                user.password_salt = null;

                user.member_since = user.created_at.Date;
                await _cache.SetObjectAsync<UsersModel>(token, user, new TimeSpan(240, 0, 0));

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
