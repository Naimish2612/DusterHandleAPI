using System.ComponentModel.DataAnnotations.Schema;
using TableAttribute = Dapper.Contrib.Extensions.TableAttribute;
namespace DUSTER.EComm.Services.Modules.RightsMasters.Models
{
    [Table("tbl_permission")]
    public class PermissionModel : BaseEntity
    {
        [Key]
        public long permission_code { get; set; }
        public string? permission_name { get; set; }
        public string? permission_description { get; set; }
        public bool is_active { get; set; }
        public bool is_block { get; set; }
        [NotMapped]
        public bool is_checked { get; set; }

    }

    public class PermissionModelValidator : AbstractValidator<PermissionModel>
    {
        public PermissionModelValidator()
        {
            RuleFor(x => x.permission_name)
                .NotEmpty().WithMessage("Permission name is required.")
                .MaximumLength(50).WithMessage("Permission name cannot exceed 50 characters.");
            RuleFor(x => x.permission_description)
                .MaximumLength(200).WithMessage("Permission description cannot exceed 200 characters.");
            RuleFor(x => x.is_active)
                .NotNull().WithMessage("Active status is required.")
                .Must(x => x == true || x == false).WithMessage("Active status must be true or false.");
        }
    }

    public class UserPermissionDTO
    {
        public List<SidebarMenuDto> sideBar { get; set; }
        public List<string> button_actions { get; set; }
    }
}
