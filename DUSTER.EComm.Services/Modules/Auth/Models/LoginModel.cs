namespace DUSTER.EComm.Services.Modules.Auth.Models
{
    public class LoginModel
    {
        public string? user_name { get; set; }
        public string? password { get; set; }
        public string? device_type { get; set; }
        public string? device_id { get; set; }
    }
}
