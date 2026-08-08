namespace DUSTER.EComm.Services.Modules.RightsMasters.Models
{
    [Table("tbl_role_permission_mapping")]
    public class RolePermissionMapping : BaseEntity
    {
        public RolePermissionMapping() { }

        [Key]
        public long role_permission_code { get; set; }
        public int role_code { get; set; } // Foreign key to RoleModel
        public long permission_code { get; set; } // Foreign key to PermissionModel

        [Computed]
        public int[] permission_codes { get; set; }
    }

    public class RolePermissionMappingValidator : AbstractValidator<RolePermissionMapping>
    {
        public RolePermissionMappingValidator()
        {
            RuleFor(x => x.role_code).NotEmpty().WithMessage("Role code is required.");
            RuleFor(x => x.permission_code).NotEmpty().WithMessage("Permission code is required.");
        }
    }
}