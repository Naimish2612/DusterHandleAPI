using DUSTER.EComm.Data.CommonClass;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DUSTER.EComm.Services.CommonServices
{
    public class JwtTokenService
    {
        private readonly IConfiguration _config;
        public JwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(CurrentUser currentUser)
        {
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Secret"]);
            var tokenHandler = new JwtSecurityTokenHandler();


            var userJson = JsonConvert.SerializeObject(currentUser); // serialize user

            var claims = new List<Claim>
            {
                new("currentUser", userJson) // inject serialized user as a single claim
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(180),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(descriptor);

            // Return the token as a string
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var key = Encoding.ASCII.GetBytes(_config["Jwt:RefreshSecret"]);
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new("uniqueCode", Guid.NewGuid().ToString("N"))
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(descriptor);

            // Return the refresh token as a string
            return tokenHandler.WriteToken(token);
        }
    }
}
