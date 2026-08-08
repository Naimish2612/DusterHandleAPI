namespace DUSTER.EComm.Services.Modules.SystemConfig.Models
{
    [Table("tbl_smtp_configuration")]
    public class SmtpConfiguration : BaseEntity
    {
        [Key]
        public int smtp_config_id { get; set; }
        public string? config_name { get; set; }
        public string? host { get; set; }
        public int port { get; set; }
        public string? username { get; set; }
        public string? password_encrypted { get; set; }
        public string? from_email { get; set; }
        public string? from_name { get; set; }
        public bool is_active { get; set; }
        public bool is_default { get; set; }
        public string smtp_category { get; set; }
    }

    public class SmtpConfigurationValidator : AbstractValidator<SmtpConfiguration>
    {
        public SmtpConfigurationValidator()
        {
            RuleFor(x => x.config_name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.host).NotEmpty().MaximumLength(255);
            RuleFor(x => x.port).GreaterThan(0);
            RuleFor(x => x.from_email).NotEmpty().EmailAddress();
        }
    }
}
