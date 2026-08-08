using DUSTER.EComm.Services.Modules.Auth.Models;
using Microsoft.AspNetCore.Http;

namespace DUSTER.EComm.Services.Modules.Auth
{
    public interface IUserServices
    {
        #region User Role Mapping
        Task<IEnumerable<UserRoleMapping>> GetUserRolesAsync(long user_code);
        Task<ResponseEntity<object>> CreateUserRoleMappingAsync(UserRoleMapping userRoleMapping);
        Task<ResponseEntity<object>> DeleteUserRoleMappingAsync(long user_role_code, long user_code, int role_code);
        #endregion

        #region User 
        Task<IActionResult> GetUsersAsync();
        Task<IActionResult> GetUserByIdAsync(long user_code);
        Task<ResponseEntity<object>> CreateUserAsync(UsersModel user);
        Task<ResponseEntity<object>> UpdateUserAsync(UsersModel user, string token = null);
        Task<ResponseEntity<object>> DeleteUserAsync(long user_code);
        Task<IActionResult> ChangePassword(ChangePasswordModel model);
        Task<IActionResult> GetUserFromCache(string token);
        Task<ResponseEntity<object>> CreateOtherUserAsync(UsersModel user);
        Task<IActionResult> EmailVerification(long user_code, string otp);
        Task<IActionResult> ResetPasswordOTPVerification(string email, string otp);
        Task<IActionResult> BlockUser(long user_code);
        Task<IActionResult> UserAddressBook(long user_code);
        Task<IActionResult> GetUserFromUserType(string user_type);

        Task<IActionResult> UploadProfilePic(IFormCollection model);
        Task<IActionResult> RemoveProfilePic(RemoveProfilePicModel model);
        #endregion

        #region User Validation

        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsMobileNoExistsAsync(string mobile_no);

        #endregion
    }
}
