using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.PrimaryDocuments.Models
{
    [Table("tbl_primary_documents")]
    public class PrimaryDocument : BaseEntity
    {

        [Key]
        public int document_id { get; set; }
        public string? document_name { get; set; }
        public string? body_html { get; set; }
        public bool? is_active { get; set; }
    }
    public class PrimaryDocumentValidator : AbstractValidator<PrimaryDocument>
    {
        public PrimaryDocumentValidator()
        {
            RuleFor(x => x.document_name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.body_html).NotEmpty();
        }
    }
}
