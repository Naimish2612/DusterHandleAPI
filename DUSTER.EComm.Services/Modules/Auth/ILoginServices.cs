using DUSTER.EComm.Services.Modules.Auth.Models;

namespace DUSTER.EComm.Services.Modules.Auth
{
    public interface ILoginServices
    {
        Task<UserToken> PortalLogin(string userName, string password);
        Task<UserToken> RefreshToken();
        Task<IActionResult> MobilesLogin(LoginModel model);
        Task<bool> SaveUserInCache(UsersModel user, string token);
    }
}
