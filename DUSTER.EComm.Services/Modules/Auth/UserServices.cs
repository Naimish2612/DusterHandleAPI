using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.CacheMemory;
using DUSTER.EComm.Data.Helpers.Cloudinary;
using DUSTER.EComm.Data.Helpers.Services.DropdownServices.Models;
using DUSTER.EComm.Data.Helpers.Strings;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.AlertEngine;
using DUSTER.EComm.Services.Modules.Auth.Models;
using DUSTER.EComm.Services.Modules.Customers.Models;
using DUSTER.EComm.Services.Modules.Notifications.Models;
using DUSTER.EComm.Services.Modules.RightsMasters.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DUSTER.EComm.Services.Modules.Auth
{
    public class UserServices : IUserServices
    {
        public readonly ICurrentUserService _currentUserService;
        private readonly IPostgresDistributedCache _cache;
        private readonly Cloudinary _cloudinary;
        private readonly IOptions<CloudinarySettings> _cloudinarySettings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        IEIPLRepository<UserRoleMapping> _userRoleMappingRepo;
        IEIPLRepository<UsersModel> _userRepo;
        IEIPLRepository<RoleModel> _roleRepo;
        IEIPLRepository<AddressBook> _addressRepo;
        IEIPLRepository<NotificationModel> _notificationRepo;

        ILoginServices _loginServices;
        private readonly IAlertEngineService _alertEngineService;

        public UserServices(IEIPLRepository<UserRoleMapping> userRoleMappingRepo, IEIPLRepository<UsersModel> userRepo, ICurrentUserService currentUserService,
            IPostgresDistributedCache cache, IEIPLRepository<RoleModel> roleRepo, IEIPLRepository<NotificationModel> notificationRepo, ILoginServices loginServices,
            IEIPLRepository<AddressBook> addressRepo, IOptions<CloudinarySettings> cloudinarySettings, IHttpContextAccessor httpContextAccessor, IAlertEngineService alertEngineService)
        {
            _userRoleMappingRepo = userRoleMappingRepo;
            _userRepo = userRepo;
            _currentUserService = currentUserService;
            _cache = cache;
            _roleRepo = roleRepo;
            _notificationRepo = notificationRepo;
            _loginServices = loginServices;
            _addressRepo = addressRepo;
            _httpContextAccessor = httpContextAccessor;
            Account account = new Account(cloudinarySettings.Value.CloudName, cloudinarySettings.Value.ApiKey, cloudinarySettings.Value.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _alertEngineService = alertEngineService;
        }

        #region User Role Mapping

        public async Task<IEnumerable<UserRoleMapping>> GetUserRolesAsync(long user_code)
        {
            var parameters = new DynamicParameters();
            parameters.Add("user_code", user_code);
            return await _userRoleMappingRepo.QueryAsync<UserRoleMapping>("SELECT * FROM tbl_user_role_mapping WHERE user_code = @user_code", parameters);
        }

        public async Task<ResponseEntity<object>> CreateUserRoleMappingAsync(UserRoleMapping userRoleMapping)
        {
            Int64 response = Convert.ToInt64(await _userRoleMappingRepo.InsertAsync(userRoleMapping));

            if (response > 0)
                return ResponseEntity<object>.Success(null, "User Role Mapping successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while creating the user role mapping.", HttpStatusCode.InternalServerError);
        }

        public async Task<ResponseEntity<object>> DeleteUserRoleMappingAsync(long user_role_code, long user_code, int role_code)
        {
            int response = Convert.ToInt32(await _userRoleMappingRepo.DeleteAsync(user_role_code, new Dictionary<string, object>
            {
                { "user_code", user_code },
                { "role_code", role_code }
            }));

            if (response > 0)
                return ResponseEntity<object>.Success(null, "User Role Mapping deleted successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while deleting the user role mapping.", HttpStatusCode.InternalServerError);
        }

        #endregion

        #region User
        public async Task<IActionResult> GetUsersAsync()
        {
            var response = await _userRepo.QueryAsync<UsersModel>("SELECT * FROM tbl_users WHERE is_active = true");

            if (response == null || !response.Any())
                return ResponseEntity<object>.Error(null, "No users found.", HttpStatusCode.InternalServerError);
            else
            {
                var allUserRoles = await _userRoleMappingRepo.QueryAsync<dynamic>("SELECT user_code, role_code FROM tbl_user_role_mapping");
                var allUserRolesList = allUserRoles.ToList();
                var returnResponse = response.Select(user => new
                {
                    user_code = user.user_code,
                    user_name = user.user_name,
                    email_id = user.email_id,
                    mobile_no = user.mobile_no,
                    user_type = user.user_type,
                    user.birthdate,
                    user.is_active,
                    user.is_block,
                    user.is_delete,
                    user.email_verify,
                    user.gender,
                    member_since = user.created_at.Date,
                    user_roles = allUserRolesList
                                .Where(x => (long)x.user_code == user.user_code)
                                .Select(x => (long)x.role_code)
                                .ToArray()

                });

                return ResponseEntity<object>.Success(returnResponse, "Users retrieved successfully.");
            }
        }

        public async Task<IActionResult> GetUserByIdAsync(long user_code)
        {
            var response = await _userRepo.GetByIdAsync(user_code);

            if (response == null)
                return ResponseEntity<object>.Error(null, "User not found.", HttpStatusCode.NotFound);
            else
                return ResponseEntity<object>.Success(response, "User retrieved successfully.");
        }

        public async Task<ResponseEntity<object>> CreateUserAsync(UsersModel user)
        {
            if (user == null)
                return ResponseEntity<object>.Error(null, "Passing object or value are null", HttpStatusCode.InternalServerError);

            if (await this.IsMobileNoExistsAsync(user.mobile_no))
                return ResponseEntity<object>.Error(null, "Mobile number already exists, please check and try again.", HttpStatusCode.InternalServerError);

            if (await this.IsEmailExistsAsync(user.email_id))
                return ResponseEntity<object>.Error(null, "Email already exists, please check and try again.", HttpStatusCode.InternalServerError);


            user.user_type = "CUSTOMER";
            user.user_name = string.IsNullOrEmpty(user.user_name) ? user.email_id : user.user_name;

            var validator = await _userRepo.ModelValidating(new ValidationModel() { ValidateModel = new UserModelValidator(), Model = user });

            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            var passwordHas = PasswordUtility.CreatePasswordHash(user.password);

            user.password_hash = passwordHas.Hash;
            user.password_salt = passwordHas.Salt;
            user.is_active = true;
            user.is_block = false;
            user.is_delete = false;
            user.allow_app_login = true;
            user.allow_web_login = true;
            user.allow_multi_login = true;

            Int64 response = Convert.ToInt64(await _userRepo.InsertAsync(user));

            if (response > 0)
            {
                List<RoleModel> fixRole = (await _roleRepo.QueryAsync<RoleModel>("SELECT * FROM tbl_role WHERE role_name='CUSTOMER_ROLE' AND is_active = true")).ToList();

                if (fixRole.Count <= 0)
                {
                    RoleModel roleModel = new RoleModel
                    {
                        role_name = "CUSTOMER_ROLE",
                        role_description = "This is a default role for customers.",
                        is_active = true,
                        is_block = false
                    };

                    int roleResponse = Convert.ToInt32(await _roleRepo.InsertAsync(roleModel));

                    if (roleResponse > 0)
                    {
                        UserRoleMapping userRoleMapping = new UserRoleMapping
                        {
                            user_code = response,
                            role_code = roleModel.role_code
                        };
                        await _userRoleMappingRepo.InsertAsync(userRoleMapping);
                    }

                    //return ResponseEntity<object>.Success(null, "User created successfully", HttpStatusCode.OK);
                }
                else
                {
                    List<UserRoleMapping> userRoleMappings = new List<UserRoleMapping>();
                    foreach (var role in fixRole)
                    {
                        UserRoleMapping userRoleMapping = new UserRoleMapping
                        {
                            user_code = response,
                            role_code = role.role_code
                        };
                        userRoleMappings.Add(userRoleMapping);
                    }

                    await _userRoleMappingRepo.InsertMultipleAsync(userRoleMappings);
                }

                await _alertEngineService.QueueEventNotificationAsync("WELCOME_EMAIL", user.email_id, user.user_name, user);
                return ResponseEntity<object>.Success(null, "User created successfully");
            }
            else
                return ResponseEntity<object>.Error(null, "An error occurred while creating the user.");
        }

        public async Task<ResponseEntity<object>> CreateOtherUserAsync(UsersModel user)
        {
            var validator = await _userRepo.ModelValidating(new ValidationModel() { ValidateModel = new UserModelValidator(), Model = user });

            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            var passwordHas = PasswordUtility.CreatePasswordHash(user.password);

            user.password_hash = passwordHas.Hash;
            user.password_salt = passwordHas.Salt;
            user.is_active = true;
            user.user_type = user.user_type;
            user.is_block = false;
            user.is_delete = false;
            user.allow_app_login = true;
            user.allow_web_login = true;
            user.allow_multi_login = true;

            Int64 response = Convert.ToInt64(await _userRepo.InsertAsync(user));

            if (response > 0)
            {
                if (user.user_roles != null && user.user_roles.Length > 0)
                {
                    List<UserRoleMapping> userRoleMappings = new List<UserRoleMapping>();
                    foreach (var role in user.user_roles)
                    {
                        UserRoleMapping userRoleMapping = new UserRoleMapping
                        {
                            user_code = response,
                            role_code = role
                        };
                        userRoleMappings.Add(userRoleMapping);
                    }

                    await _userRoleMappingRepo.InsertMultipleAsync(userRoleMappings);

                    return ResponseEntity<object>.Success(null, "User created successfully");
                }
                else
                {

                    string role_name = string.Empty;
                    if (user.user_type == "ADMIN")
                        role_name = "ADMIN_ROLE";
                    else
                        role_name = "CUSTOMER_ROLE";

                    List<RoleModel> fixRole = (await _roleRepo.QueryAsync<RoleModel>($"SELECT * FROM tbl_role WHERE role_name='{role_name}' AND is_active = true")).ToList();

                    if (fixRole.Count <= 0)
                    {
                        RoleModel roleModel = new RoleModel
                        {
                            role_name = role_name,
                            role_description = $"This is a default role for {role_name}.",
                            is_active = true,
                            is_block = false
                        };

                        int roleResponse = Convert.ToInt32(await _roleRepo.InsertAsync(roleModel));

                        if (roleResponse > 0)
                        {
                            UserRoleMapping userRoleMapping = new UserRoleMapping
                            {
                                user_code = response,
                                role_code = roleResponse
                            };
                            await _userRoleMappingRepo.InsertAsync(userRoleMapping);
                        }

                        return ResponseEntity<object>.Success(null, "User created successfully");
                    }
                    else
                    {
                        List<UserRoleMapping> userRoleMappings = new List<UserRoleMapping>();
                        foreach (var role in fixRole)
                        {
                            UserRoleMapping userRoleMapping = new UserRoleMapping
                            {
                                user_code = response,
                                role_code = role.role_code
                            };
                            userRoleMappings.Add(userRoleMapping);
                        }

                        await _userRoleMappingRepo.InsertMultipleAsync(userRoleMappings);

                        return ResponseEntity<object>.Success(null, "User created successfully");
                    }
                }
            }
            else
                return ResponseEntity<object>.Error(null, "An error occurred while creating the user.");
        }

        public async Task<ResponseEntity<object>> UpdateUserAsync(UsersModel user, string token = null)
        {
            if (user == null)
                return ResponseEntity<object>.Error(null, "Passing object or value are null");

            UsersModel userData = await _userRepo.GetByIdAsync(user.user_code);

            if (userData == null)
                return ResponseEntity<object>.Error(null, "User not found, please contact your administrator.");

            userData.gender = user.gender;
            userData.birthdate = user.birthdate;
            userData.full_name = user.full_name;

            if (userData.mobile_no != user.mobile_no)
            {
                userData.mobile_no = user.mobile_no;
                userData.phone_verify = false;
            }

            if (userData.email_id != user.email_id)
            {
                userData.email_id = user.email_id;
                userData.email_verify = false;
            }

            int response = await _userRepo.UpdateAsync(userData);

            if (response > 0)
            {
                if (user.user_roles != null)
                {
                    var param = new DynamicParameters();
                    param.Add("userCode", user.user_code);

                    string existingRolesQuery = "SELECT role_code FROM tbl_user_role_mapping WHERE user_code = @userCode";
                    IEnumerable<long> existingRoleCodes = await _userRoleMappingRepo.QueryAsync<long>(existingRolesQuery, param);

                    HashSet<long> databaseSet = [.. existingRoleCodes];
                    HashSet<long> payloadSet = [.. user.user_roles.Select(x => (long)x)];

                    List<long> rolesToDelete = [.. databaseSet.Where(role => !payloadSet.Contains(role))];

                    List<long> rolesToInsert = [.. payloadSet.Where(role => !databaseSet.Contains(role))];

                    if (rolesToDelete.Count > 0)
                    {
                        var deleteParam = new DynamicParameters();
                        deleteParam.Add("userCode", user.user_code);
                        deleteParam.Add("rolesToDelete", rolesToDelete);

                        string deleteQuery = "DELETE FROM tbl_user_role_mapping WHERE user_code = @userCode AND role_code = ANY(@rolesToDelete)";
                        await _userRoleMappingRepo.QueryAsync<int>(deleteQuery, deleteParam);
                    }

                    if (rolesToInsert.Count > 0)
                    {
                        List<UserRoleMapping> urmList = [];
                        foreach (var roleCode in rolesToInsert)
                        {
                            UserRoleMapping newMapping = new()
                            {
                                user_code = user.user_code,
                                role_code = (int)roleCode
                            };
                            urmList.Add(newMapping);
                        }

                        await _userRoleMappingRepo.InsertMultipleAsync(urmList);
                    }
                }

                if (token != null)
                {
                    await _cache.RemoveAsync(token);

                    userData.password = null;
                    userData.password_hash = null;
                    userData.password_salt = null;
                    userData.member_since = userData.created_at.Date;
                    await _cache.SetObjectAsync<UsersModel>(token, userData, new TimeSpan(240, 0, 0));
                }

                return ResponseEntity<object>.Success(null, "User updated successfully");
            }
            else
            {
                return ResponseEntity<object>.Error(null, "An error occurred while updating the user.");
            }
        }


        public async Task<ResponseEntity<object>> DeleteUserAsync(long user_code)
        {

            UsersModel userData = await _userRepo.GetByIdAsync(user_code);

            if (userData == null)
                return ResponseEntity<object>.Error(null, "User not found, please contact your administrator.", HttpStatusCode.InternalServerError);


            userData.is_active = false;
            userData.is_delete = true;

            int response = await _userRepo.UpdateAsync(userData);

            if (response > 0)
                return ResponseEntity<object>.Success(null, "User deleted successfully");
            else
                return ResponseEntity<object>.Error(null, "An error occurred while deleting the user.");
        }

        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            try
            {
                if (model.user_code > 0 && string.IsNullOrEmpty(model.new_password))
                    return ResponseEntity<object>.Error(null, "Passing value or object are null or empty.");

                UsersModel userData = await _userRepo.GetByIdAsync(model.user_code);

                if (userData == null)
                    return ResponseEntity<object>.Error(null, "User not found, please contact your administrator.");

                if (string.IsNullOrEmpty(model.password) && !string.IsNullOrEmpty(model.new_password))
                {
                    var (Hash, Salt) = PasswordUtility.CreatePasswordHash(model.new_password);

                    userData.password_salt = Salt;
                    userData.password_hash = Hash;

                    int response1 = await _userRepo.UpdateAsync(userData);

                    if (response1 > 0)
                    {
                        await _alertEngineService.QueueEventNotificationAsync("CHANGE_PASSWORD_MAIL", userData.email_id, userData.user_name, userData);
                        return ResponseEntity<object>.Success(null, "Password Change Successfully");
                    }
                    else
                        return ResponseEntity<object>.Error(null, "An error occurred while change the password.");
                }

                bool passwordMatch = PasswordUtility.VerifyPassword(model.password, userData.password_hash, userData.password_salt);

                if (!passwordMatch)
                    return ResponseEntity<object>.Error(null, "Invalid Password");

                var passwordHas = PasswordUtility.CreatePasswordHash(model.new_password);

                userData.password_salt = passwordHas.Salt;
                userData.password_hash = passwordHas.Hash;

                int response = await _userRepo.UpdateAsync(userData);

                if (response > 0)
                {
                    await _alertEngineService.QueueEventNotificationAsync("CHANGE_PASSWORD_MAIL", userData.email_id, userData.user_name, userData);
                    return ResponseEntity<object>.Success(null, "Password Change Successfully");
                }
                else
                {
                    return ResponseEntity<object>.Error(null, "An error occurred while change the password.");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IActionResult> GetUserFromCache(string token)
        {
            try
            {
                var user = await _cache.GetObjectAsync<UsersModel>(token);

                if (user == null)
                    return ResponseEntity<object>.Error(null, "User not found");
                else
                {

                    object response = new
                    {
                        user_code = user.user_code,
                        user_name = user.user_name,
                        full_name = user.full_name,
                        email_id = user.email_id,
                        mobile_no = user.mobile_no,
                        user_type = user.user_type,
                        profile_photo_url = user.profile_photo_url,
                        user.birthdate,
                        user.gender,
                        member_since = user.created_at.ToString("yyyy-MM-dd"),
                        email_verify = user.email_verify,
                        phone_verify = user.phone_verify

                    };

                    return ResponseEntity<object>.Success(response, "User retrieved from cache successfully.");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IActionResult> EmailVerification(long user_code, string otp)
        {
            try
            {
                if (user_code <= 0)
                    return ResponseEntity<object>.Error(null, "User code is required for email verification.");

                if (string.IsNullOrEmpty(otp))
                    return ResponseEntity<object>.Error(null, "OTP is required for email verification.");

                UsersModel userData = await _userRepo.GetByIdAsync(user_code);

                if (userData == null)
                    return ResponseEntity<object>.Error(null, "User not found, please contact your administrator.");

                var parameters = new DynamicParameters();
                parameters.Add("user_code", user_code);
                parameters.Add("otp", otp);

                var notification = await _notificationRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_notification WHERE user_code = @user_code AND notification_value = @otp AND status =0 ", parameters);

                if (notification == null)
                    return ResponseEntity<object>.Error(null, "Invalid OTP or OTP has been used, please check and try again.");

                notification.status = 1;

                var notificationUpdateResponse = await _notificationRepo.UpdateAsync(notification);

                userData.email_verify = true;

                await _alertEngineService.QueueEventNotificationAsync("VERIFICATION_EMAIL", userData.email_id, userData.user_name, userData);
                int response = await _userRepo.UpdateAsync(userData);

                if (response == 0)
                    return ResponseEntity<object>.Error(null, "An error occurred while verifying the email.");
                else
                {
                    await UpdateUserCacheForActiveTokens(userData);
                    return ResponseEntity<object>.Success(null, "Email verified successfully");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> ResetPasswordOTPVerification(string email, string otp)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return ResponseEntity<object>.Error(null, "Email is required for email verification.");

                if (string.IsNullOrEmpty(otp))
                    return ResponseEntity<object>.Error(null, "OTP is required for email verification.");

                var param = new DynamicParameters();
                param.Add("email", email);

                UsersModel userData = await _userRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_users WHERE email_id = @email", param);

                if (userData == null)
                    return ResponseEntity<object>.Error(null, "User not found, please contact your administrator.");

                var parameters = new DynamicParameters();
                parameters.Add("user_code", userData.user_code);
                parameters.Add("otp", otp);

                var notification = await _notificationRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_notification WHERE user_code = @user_code AND notification_value = @otp AND status =0 ", parameters);

                if (notification == null)
                    return ResponseEntity<object>.Error(null, "Invalid/Expired OTP or OTP has been used, please check and try again.");

                notification.status = 1;

                var notificationUpdateResponse = await _notificationRepo.UpdateAsync(notification);

                if (notificationUpdateResponse == 0)
                    return ResponseEntity<object>.Error(null, "An error occurred while verifying the OTP.");
                else
                    return ResponseEntity<object>.Success(new { user_code = userData.user_code }, "OTP verified successfully");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> BlockUser(long user_code)
        {
            try
            {
                if (user_code <= 0)
                    return ResponseEntity<object>.Error(null, "User code is required for block the user.");
                UsersModel userData = await _userRepo.GetByIdAsync(user_code);

                if (userData == null)
                    return ResponseEntity<object>.Error(null, "User not found, please contact your administrator.");

                if (userData.is_delete == true)
                    return ResponseEntity<object>.Error(null, "User is already deleted, please contact your administrator.");

                if (userData.is_block == true)
                    userData.is_block = false;
                else
                    userData.is_block = true;

                string message = userData.is_block == true ? "User blocked successfully." : "User unblocked successfully.";

                int response = await _userRepo.UpdateAsync(userData);

                if (response == 0)
                    return ResponseEntity<object>.Error(null, "An error occurred while blocking the user.");
                else
                {
                    if (userData.is_block)
                    {
                        await _alertEngineService.QueueEventNotificationAsync("BLOCK_USER", userData.email_id, userData.user_name, userData);
                    }
                    else
                    {
                        await _alertEngineService.QueueEventNotificationAsync("UNBLOCK_USER", userData.email_id, userData.user_name, userData);
                    }
                    return ResponseEntity<object>.Success(null, message);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> UserAddressBook(long user_code)
        {
            try
            {
                if (user_code <= 0)
                    return ResponseEntity<object>.Error(null, "User code is required for block the user.");
                UsersModel userData = await _userRepo.GetByIdAsync(user_code);
                if (userData == null || userData.is_active == false || userData.is_block == true)
                {
                    return ResponseEntity<object>.Error(null, "User not found, please contact your administrator.");
                }
                List<AddressBook> addresslist = (await _addressRepo.QueryAsync<AddressBook>($"SELECT * FROM tbl_customer_address_book WHERE user_code='{user_code}'")).ToList();
                if (addresslist.Count < 1)
                {
                    return ResponseEntity<object>.Error(addresslist, "The User does not have any address saved", HttpStatusCode.InternalServerError);
                }
                return ResponseEntity<object>.Success(addresslist);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetUserFromUserType(string user_type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user_type))
                {
                    return ResponseEntity<object>.Error(null, "User Type is required");
                }

                var filters = new Dictionary<string, object>
                {
                    { "is_active", true },
                    { "is_block", false }
                };

                if (!user_type.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                    filters.Add("user_type", user_type.Trim());

                var userData = await _userRepo.GetDropdownAsync(new DropdownRequestModel() { table_name = "tbl_users", table_columns = "user_code as id, user_name || '[' || user_type || ']' as value", StaticFilters = filters });

                if (userData.Count() <= 0)
                    return ResponseEntity<object>.Error(null, "Users data not available");
                else
                    return ResponseEntity<object>.Success(userData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region User Validation Services

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("email", email);
            var user = await _userRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_users WHERE email_id = @email", parameters);
            return user != null;
        }

        public async Task<bool> IsMobileNoExistsAsync(string mobile_no)
        {
            var parameters = new DynamicParameters();
            parameters.Add("mobile_no", mobile_no);
            var user = await _userRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_users WHERE mobile_no = @mobile_no", parameters);
            return user != null;
        }

        #endregion

        #region User Profile pic

        public async Task<IActionResult> UploadProfilePic(IFormCollection model)
        {
            try
            {
                if (model == null || !model.ContainsKey("data"))
                    return ResponseEntity<object>.Error(null, "Form data is missing or invalid.");

                if (model.Files.Count <= 0)
                    return ResponseEntity<object>.Error(null, "Profile picture is required.");

                var data = model["data"].ToString();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var pm = System.Text.Json.JsonSerializer.Deserialize<ProfilePicUpdateModel>(data, options);

                if (pm == null)
                    return ResponseEntity<object>.Error(null, "Failed to parse profile picture data.");

                // Validate model
                var validator = await _userRepo.ModelValidating(new ValidationModel()
                {
                    ValidateModel = new ProfilePicUpdateValidator(),
                    Model = pm
                });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                // Validate file (type & size)
                var fileValidation = ValidateImageFile(model.Files[0]);
                if (!string.IsNullOrEmpty(fileValidation))
                    return ResponseEntity<object>.Error(null, fileValidation);

                // Get existing user using generic repository
                UsersModel existingUser = await _userRepo.GetByIdAsync(pm.user_code);

                if (existingUser == null)
                    return ResponseEntity<object>.Error(null, "User not found, please contact your administrator.");

                // Save old URL for cleanup
                var oldPhotoUrl = existingUser.profile_photo_url;

                // Upload to Cloudinary
                var imageData = await UploadProfileImage(model.Files);

                if (imageData.image_public_id == null && imageData.image_url == null)
                    return ResponseEntity<object>.Error(null, "Profile picture upload failed, Please contact your Administrator.");

                // Update user
                existingUser.profile_photo_url = imageData.image_url;

                long response = Convert.ToInt64(await _userRepo.UpdateAsync(existingUser));

                if (response > 0)
                {
                    // Delete old image from Cloudinary AFTER DB update succeeds
                    if (!string.IsNullOrEmpty(oldPhotoUrl))
                    {
                        var oldPublicId = ExtractPublicIdFromUrl(oldPhotoUrl);
                        if (!string.IsNullOrEmpty(oldPublicId))
                            await DeleteCloudinaryImage(oldPublicId);
                    }

                    await UpdateUserCacheForActiveTokens(existingUser);

                    return ResponseEntity<object>.Success(new
                    {
                        user_code = pm.user_code,
                        profile_photo_url = imageData.image_url
                    }, "Profile picture updated successfully.");
                }
                else
                {
                    // Rollback: delete just-uploaded Cloudinary image since DB failed
                    if (!string.IsNullOrEmpty(imageData.image_public_id))
                        await DeleteCloudinaryImage(imageData.image_public_id);

                    return ResponseEntity<object>.Error(null, "An error occurred while updating profile picture.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> RemoveProfilePic(RemoveProfilePicModel pm)
        {
            try
            {
                if (pm == null)
                    return ResponseEntity<object>.Error(null, "Request data is missing.");

                // Validate
                var validator = await _userRepo.ModelValidating(new ValidationModel()
                {
                    ValidateModel = new RemoveProfilePicValidator(),
                    Model = pm
                });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                // Get existing user using generic repository
                UsersModel existingUser = await _userRepo.GetByIdAsync(pm.user_code);

                if (existingUser == null)
                    return ResponseEntity<object>.Error(null, "User not found, please contact your administrator.");

                if (string.IsNullOrEmpty(existingUser.profile_photo_url))
                    return ResponseEntity<object>.Error(null, "No profile picture to remove.");

                // Save old URL for cleanup
                var oldPhotoUrl = existingUser.profile_photo_url;

                // Clear profile pic
                existingUser.profile_photo_url = null;

                long response = Convert.ToInt64(await _userRepo.UpdateAsync(existingUser));

                if (response > 0)
                {
                    // Delete from Cloudinary
                    var oldPublicId = ExtractPublicIdFromUrl(oldPhotoUrl);
                    if (!string.IsNullOrEmpty(oldPublicId))
                        await DeleteCloudinaryImage(oldPublicId);

                    await UpdateUserCacheForActiveTokens(existingUser);

                    return ResponseEntity<object>.Success(new
                    {
                        user_code = pm.user_code
                    }, "Profile picture removed successfully.");
                }
                else
                {
                    return ResponseEntity<object>.Error(null, "An error occurred while removing profile picture.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<(string? image_url, string? image_public_id)> UploadProfileImage(IFormFileCollection files)
        {
            try
            {
                IFormFile _file = files[0];

                using var stream = _file.OpenReadStream();

                string file_name = $"{StringHelper.GetUniqueString(10)}{Path.GetExtension(files[0].FileName)}";

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file_name, stream),
                    Folder = "EComm/profile-pics",
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false,
                    Transformation = new Transformation()
                        .Width(400)
                        .Height(400)
                        .Crop("fill")
                        .Gravity("face")
                        .Quality("auto:good")
                        .FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK
                    && uploadResult.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    return (null, null);
                }

                var publicUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Uri?.ToString();
                return (publicUrl, uploadResult.PublicId);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string? ExtractPublicIdFromUrl(string? url)
        {
            try
            {
                if (string.IsNullOrEmpty(url)) return null;

                var uploadIndex = url.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
                if (uploadIndex < 0) return null;

                var afterUpload = url.Substring(uploadIndex + "/upload/".Length);

                var segments = afterUpload.Split('/');
                int startIndex = 0;
                if (segments.Length > 0 && segments[0].StartsWith("v") && segments[0].Length > 1 && long.TryParse(segments[0].Substring(1), out _))
                {
                    startIndex = 1;
                }

                var publicIdWithExt = string.Join("/", segments.Skip(startIndex));

                var lastDotIndex = publicIdWithExt.LastIndexOf('.');
                if (lastDotIndex > 0)
                    publicIdWithExt = publicIdWithExt.Substring(0, lastDotIndex);

                return publicIdWithExt;
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> DeleteCloudinaryImage(string publicId)
        {
            try
            {
                var deletionParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };
                var result = await _cloudinary.DestroyAsync(deletionParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }

        private string ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "No file uploaded.";

            const long maxSize = 2 * 1024 * 1024; // 2MB
            if (file.Length > maxSize)
                return "File size must be less than 2MB.";

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
            string fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                return "Only JPG, JPEG, PNG, and WEBP files are allowed.";

            string[] allowedMimeTypes = { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
                return "Invalid file type.";

            return string.Empty;
        }

        private async Task UpdateUserCacheForActiveTokens(UsersModel userData)
        {
            try
            {
                var tokensToUpdate = new HashSet<string>();

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null && httpContext.Request.Headers.TryGetValue("Authentication", out var authHeaderValue))
                {
                    string? currentToken = authHeaderValue.ToString();
                    if (!string.IsNullOrEmpty(currentToken))
                    {
                        tokensToUpdate.Add(currentToken);
                    }
                }

                var parameters = new DynamicParameters();
                parameters.Add("user_code", userData.user_code);
                var dbTokens = await _userRepo.QueryAsync<string>("SELECT access_token FROM tbl_user_token WHERE user_code = @user_code AND is_active = true", parameters);

                if (dbTokens != null)
                {
                    foreach (var token in dbTokens)
                    {
                        if (!string.IsNullOrEmpty(token))
                        {
                            tokensToUpdate.Add(token);
                        }
                    }
                }

                foreach (var token in tokensToUpdate)
                {
                    await _cache.RemoveAsync(token);

                    userData.password = null;
                    userData.password_hash = null;
                    userData.password_salt = null;
                    userData.member_since = userData.created_at.Date;
                    await _cache.SetObjectAsync<UsersModel>(token, userData, new TimeSpan(240, 0, 0));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateUserCache] ERROR: {ex.Message}");
                throw;
            }
        }

        #endregion

    }
}
