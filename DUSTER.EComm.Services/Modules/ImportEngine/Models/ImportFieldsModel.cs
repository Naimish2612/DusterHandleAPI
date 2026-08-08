using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.ImportEngine.Models
{
    [Table("tbl_import_process_configuration")]
    public class ImportFieldsModel : BaseEntity
    {
        [Key]
        public int import_id { get; set; }
        public string? process_name { get; set; }
        public string[]? required_fields { get; set; }
        public string[]? non_required_fields { get; set; }
        public int? max_records_allowed { get; set; }
        public bool is_active { get; set; } = true;
        public bool is_delete { get; set; } = false;
    }

    public class ImportFieldsModelValidator : AbstractValidator<ImportFieldsModel>
    {
        public ImportFieldsModelValidator()
        {
            RuleFor(x => x.process_name).NotEmpty().WithMessage("Import name is required.");
        }
    }

}
