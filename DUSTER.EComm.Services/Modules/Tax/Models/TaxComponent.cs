namespace DUSTER.EComm.Services.Modules.Tax.Models
{
    [Table("tbl_tax_components")]
    public class TaxComponent : BaseEntity
    {
        [Key]
        public int component_id { get; set; }
        public string? component_name { get; set; }
        public bool is_active { get; set; } = true;
    }

    public class TaxComponentValidator : AbstractValidator<TaxComponent>
    {
        public TaxComponentValidator()
        {
            RuleFor(x => x.component_name).NotEmpty().WithMessage("Component name is required.");
        }
    }
}
