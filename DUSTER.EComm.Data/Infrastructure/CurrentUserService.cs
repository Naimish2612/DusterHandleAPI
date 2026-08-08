using DUSTER.EComm.Data.CommonClass;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace DUSTER.EComm.Data.Infrastructure
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public CurrentUserService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public CurrentUser? User
        {
            get
            {
                var userJson = _contextAccessor.HttpContext?.User.FindFirst("currentUser")?.Value;
                if (string.IsNullOrEmpty(userJson))
                {
                    CurrentUser currentUser = new CurrentUser
                    {
                        user_code = 0,
                        user_name = "EIPL",
                        mobile_no = string.Empty,
                        email = "EIPL@DUSTER.COM"
                    };

                    return currentUser;
                }

                return JsonConvert.DeserializeObject<CurrentUser>(userJson);
            }
        }
    }
}
