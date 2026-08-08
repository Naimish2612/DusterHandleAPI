namespace DUSTER.EComm.Services.Modules.Auth.Models
{
    [Table("tbl_user_role_mapping")]
    public class UserRoleMapping : BaseEntity
    {
        [Key] // Assuming this is the primary key for the mapping table
        public long user_role_code { get; set; }
        public long user_code { get; set; } // Foreign key to UsersModel
        public int role_code { get; set; } // Foreign key to RoleModel
    }

    public class UserRoleMappingValidator : AbstractValidator<UserRoleMapping>
    {
        public UserRoleMappingValidator()
        {
            RuleFor(x => x.user_code).NotEmpty().WithMessage("User code is required.");
            RuleFor(x => x.role_code).NotEmpty().WithMessage("Role code is required.");
        }
    }
}
