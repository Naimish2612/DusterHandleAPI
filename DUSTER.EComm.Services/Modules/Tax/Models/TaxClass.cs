namespace DUSTER.EComm.Services.Modules.Tax.Models
{
    [Table("tbl_tax_classes")]
    public class TaxClass : BaseEntity
    {
        [Key]
        public int class_id { get; set; }
        public string? class_name { get; set; }
        public bool is_active { get; set; } = true;
    }

    public class TaxClassValidator : AbstractValidator<TaxClass>
    {
        public TaxClassValidator()
        {
            RuleFor(x => x.class_name).NotEmpty().WithMessage("Class name is required.");
        }
    }
}
