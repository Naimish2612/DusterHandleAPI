namespace DUSTER.EComm.Services.Modules.Auth.Models
{
    [Table("tbl_user_token")]
    public class UserToken : BaseEntity
    {
        public UserToken()
        {

        }

        [Key]
        public long user_token_code { get; set; }
        public long user_code { get; set; }
        public string? access_token { get; set; }
        public string? refresh_token { get; set; }
        public DateTime access_token_expiry { get; set; }
        public DateTime refresh_token_expiry { get; set; }
        public string? device_info { get; set; }
        public string? ip_address { get; set; }
        public bool is_active { get; set; }
        public string? device_type { get; set; }
        public string? device_id { get; set; }
        [Computed]
        public string? user_type { get; set; }
    }
}
