using DUSTER.EComm.Services.Modules.Auth;
using DUSTER.EComm.Services.Modules.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace DUSTER.EComm.API.Modules.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserServices _userServices;
        public UserController(IUserServices userServices)
        {
            _userServices = userServices;
        }

        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateUser([FromBody] UsersModel userModel)
        {
            try
            {
                return await _userServices.CreateUserAsync(userModel);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("change/password")]
        [AllowAnonymous]
        public async Task<IActionResult> ChangePassowrd([FromBody] ChangePasswordModel userModel)
        {
            try
            {
                return await _userServices.ChangePassword(userModel);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveUser([FromBody] UsersModel usersModel)
        {
            try
            {
                if (usersModel.user_code <= 0)
                    return ResponseEntity<object>.Error(null, "Passing object or value are null or empty.", System.Net.HttpStatusCode.InternalServerError);

                return await _userServices.DeleteUserAsync(usersModel.user_code);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("get/user")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                string token = Request.Headers["Authentication"];

                return await _userServices.GetUserFromCache(token);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("create/other/user")]
        public async Task<IActionResult> CreateOtherUser([FromBody] UsersModel userModel)
        {
            try
            {
                return await _userServices.CreateOtherUserAsync(userModel);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("email/verification")]
        public async Task<IActionResult> EmailVerification(long user_code, string otp)
        {
            try
            {
                return await _userServices.EmailVerification(user_code, otp);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("password/reset/otp/verification")]
        [AllowAnonymous]
        public async Task<IActionResult> PasswordResetOTPVerification(string email, string otp)
        {
            try
            {
                return await _userServices.ResetPasswordOTPVerification(email, otp);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("profile/update")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UsersModel userModel)
        {
            try
            {
                string token = Request.Headers["Authentication"];

                return await _userServices.UpdateUserAsync(userModel, token);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUserList()
        {
            try
            {
                return await _userServices.GetUsersAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("block/{user_code}")]
        public async Task<IActionResult> BlockUser(long user_code)
        {
            try
            {
                return await _userServices.BlockUser(user_code);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("get/user/from/usertype/{user_type}")]
        public async Task<IActionResult> GetUserFromUserType(string user_type)
        {
            try
            {
                return await _userServices.GetUserFromUserType(user_type);
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        [HttpPost("upload/profile-pic")]
        public async Task<IActionResult> UploadProfilePic(IFormCollection model)
        {
            try
            {
                return await _userServices.UploadProfilePic(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("remove/profile-pic")]
        public async Task<IActionResult> RemoveProfilePic(RemoveProfilePicModel model)
        {
            try
            {
                return await _userServices.RemoveProfilePic(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
