using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using DUSTER.EComm.Data.CommonClass;

namespace DUSTER.EComm.Data.Helpers.Filters
{
    public class JwtAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IConfiguration _config;
        public JwtAuthorizationFilter(IConfiguration config)
        {
            _config = config;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {

            var endpoint = context.ActionDescriptor as ControllerActionDescriptor;
            if (endpoint?.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any() == true ||
                endpoint.ControllerTypeInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any())
            {
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue("Authentication", out var tokenHeader))
            {
                context.Result = ResponseEntity<string>.Error(null, "Token is missing", HttpStatusCode.Unauthorized);
                return;
            }

            var token = tokenHeader.FirstOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                context.Result = ResponseEntity<string>.Error(null, "Token is missing", HttpStatusCode.Unauthorized);
                return;
            }

            try
            {
                var key = Encoding.ASCII.GetBytes(_config["Jwt:Secret"]);
                var tokenHandler = new JwtSecurityTokenHandler();

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // no extra grace period for token expiration
                }, out var validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    context.Result = ResponseEntity<string>.Error(null, "Token has expired", HttpStatusCode.Unauthorized);
                    return;
                }

                var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
                context.HttpContext.User = new ClaimsPrincipal(identity);
            }
            catch (SecurityTokenExpiredException)
            {
                context.Result = ResponseEntity<string>.Error(null, "Token has expired", HttpStatusCode.Unauthorized);
            }
            catch (SecurityTokenValidationException)
            {
                context.Result = ResponseEntity<string>.Error(null, "Token validation failed", HttpStatusCode.Unauthorized);
            }
            catch (Exception)
            {
                context.Result = ResponseEntity<string>.Error(null, "Invalid token", HttpStatusCode.Unauthorized);
            }
        }
    }
}
