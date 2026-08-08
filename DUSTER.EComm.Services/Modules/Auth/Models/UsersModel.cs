namespace DUSTER.EComm.Services.Modules.Auth.Models
{
    [Table("tbl_users")]
    public class UsersModel : BaseEntity
    {
        public UsersModel()
        {

        }

        [Key]
        public long user_code { get; set; }
        public string? user_name { get; set; }
        public string? mobile_no { get; set; }
        public string? email_id { get; set; }
        /// <summary>
        /// CUSTOMER, ADMIN,SUPER_ADMIN
        /// </summary>
        public string? user_type { get; set; }
        public bool is_active { get; set; }
        public bool is_block { get; set; }
        public bool allow_app_login { get; set; }
        public bool allow_web_login { get; set; }
        public bool allow_multi_login { get; set; }
        [Computed]
        public string? password { get; set; }
        public string? password_hash { get; set; } // Nullable to allow for creation without a password
        public string? password_salt { get; set; } // Nullable to allow for creation without a password
        public string? profile_photo_url { get; set; }
        [Computed]
        public int[]? user_roles { get; set; }
        public bool is_delete { get; set; }
        public bool email_verify { get; set; }
        public bool phone_verify { get; set; }
        public string? gender { get; set; }
        public DateTime birthdate { get; set; }
        [Computed]
        public DateTime? member_since { get; set; }
        public string? full_name { get; set; }
        [Computed]
        public string? shipping_address { get; set; }
        [Computed]
        public string? billing_address { get; set; }

    }

    public class UserModelValidator : AbstractValidator<UsersModel>
    {
        public UserModelValidator()
        {
            RuleFor(x => x.user_name)
                .NotEmpty().WithMessage("User name is required.")
                .MaximumLength(50).WithMessage("User name cannot exceed 50 characters.");
            RuleFor(x => x.mobile_no)
                .NotEmpty().WithMessage("Mobile number is required.")
                .Matches(@"^\d{10}$").WithMessage("Mobile number must be 10 digits.");
            RuleFor(x => x.email_id)
                .NotEmpty().WithMessage("Email ID is required.")
                .EmailAddress().WithMessage("Invalid email format.");
            RuleFor(x => x.user_type).NotEmpty().WithMessage("User type is required.");

        }
    }

    public class UserFilterModel
    {
        public string? email_id { get; set; }
        public string? mobile_no { get; set; }
        public string? user_name { get; set; }
        public string? user_type { get; set; }

    }

    public class ProfilePicUpdateModel : BaseEntity
    {
        public long user_code { get; set; }
    }

    public class RemoveProfilePicModel : BaseEntity
    {
        public long user_code { get; set; }
    }

    public class ProfilePicUpdateValidator : AbstractValidator<ProfilePicUpdateModel>
    {
        public ProfilePicUpdateValidator()
        {
            RuleFor(x => x.user_code).NotEmpty().WithMessage("user_code is required.");
        }
    }

    public class RemoveProfilePicValidator : AbstractValidator<RemoveProfilePicModel>
    {
        public RemoveProfilePicValidator()
        {
            RuleFor(x => x.user_code).NotEmpty().WithMessage("user_code is required.");
        }
    }

    public class ChangePasswordModel : BaseEntity
    {
        public long user_code { get; set; }
        public string? password { get; set; }
        public string new_password { get; set; }

    }
}
