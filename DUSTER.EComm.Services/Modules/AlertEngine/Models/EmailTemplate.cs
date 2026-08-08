using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.AlertEngine.Models
{
    [Table("tbl_email_template")]
    public class EmailTemplate : BaseEntity
    {
        [Key]
        public int template_id { get; set; }
        public string? event_code { get; set; }
        public string? subject_template { get; set; }
        public string? body_html { get; set; }
        public bool is_active { get; set; }
        public int? smtp_config_id { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "jsonb")]
        public string? required_template_fields { get; set; }
        [Computed]
        public object? required_template_fields_obj { get; set; }
    }

    public class EmailTemplateValidator : AbstractValidator<EmailTemplate>
    {
        public EmailTemplateValidator()
        {
            RuleFor(x => x.event_code).NotEmpty().MaximumLength(100);
            RuleFor(x => x.subject_template).NotEmpty().MaximumLength(500);
            RuleFor(x => x.body_html).NotEmpty();
        }
    }
}
