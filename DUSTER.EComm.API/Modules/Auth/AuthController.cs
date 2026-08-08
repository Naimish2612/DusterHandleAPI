using DUSTER.EComm.Data.Helpers.Logger;
using DUSTER.EComm.Services.Modules.Auth;
using DUSTER.EComm.Services.Modules.Auth.Models;
using Microsoft.AspNetCore.Authorization;

namespace DUSTER.EComm.API.Modules.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILoginServices _loginServices;
        private readonly IErrorLogger _log;
        public AuthController(ILoginServices loginServices,IErrorLogger log)
        {
            _loginServices = loginServices;
            _log = log;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                _log.Log("Welcome to AuthController Login method");
                if (!string.IsNullOrEmpty(model.user_name) && !string.IsNullOrEmpty(model.password))
                {

                    UserToken currentToken = await _loginServices.PortalLogin(model.user_name, model.password);

                    if (currentToken != null)
                    {
                        Response.Cookies.Append("refreshToken", currentToken.refresh_token, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true, // Set to true if using HTTPS
                            Expires = currentToken.refresh_token_expiry, // Set expiration for refresh token
                            SameSite = SameSiteMode.Strict, // Set SameSite policy for security
                            IsEssential = true, // Make cookie essential for the application
                        });

                        return ResponseEntity<object>.Success(new { token = currentToken.access_token, expiresAt = currentToken.access_token_expiry, userType = currentToken.user_type, }, "Login Successful");
                    }
                    else
                        return ResponseEntity<object>.Error(null, "Invalid Username or Password", HttpStatusCode.InternalServerError);
                }

                return ResponseEntity<object>.Error(null, "Invalid Username or Password", HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("refresh/token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            UserToken response = await _loginServices.RefreshToken();

            if (response != null)
            {
                Response.Cookies.Append("refreshToken", response.refresh_token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // Set to true if using HTTPS
                    Expires = response.refresh_token_expiry, // Set expiration for refresh token
                    SameSite = SameSiteMode.Strict, // Set SameSite policy for security
                    IsEssential = true, // Make cookie essential for the application
                });
            }

            // Return the new token
            return ResponseEntity<object>.Success(new { token = response.access_token, expiresAt = response.access_token_expiry }, "Token refreshed successfully");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            try
            {
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
