namespace DUSTER.EComm.Services.Modules.RightsMasters.Models
{
    [Table("tbl_role")]
    public class RoleModel : BaseEntity
    {
        [Key]
        public int role_code { get; set; }
        public string? role_name { get; set; }
        public string? role_description { get; set; }
        public Boolean? is_active { get; set; }
        public Boolean? is_block { get; set; }
    }

    public class RoleModelValidator : AbstractValidator<RoleModel>
    {
        public RoleModelValidator()
        {
            RuleFor(RuleFor => RuleFor.role_name)
                .NotEmpty().WithMessage("Role name is required.")
                .MaximumLength(30).WithMessage("Role name cannot exceed 30 characters.");

            RuleFor(RuleFor => RuleFor.role_description)
                .MaximumLength(100).WithMessage("Role description cannot exceed 100 characters.");

            RuleFor(RuleFor => RuleFor.is_active)
                .NotNull().WithMessage("Active status is required.")
                .Must(x => x == true || x == false).WithMessage("Active status must be true or false.");
        }
    }
}
