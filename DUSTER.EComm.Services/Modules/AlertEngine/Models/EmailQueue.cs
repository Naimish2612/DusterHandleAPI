namespace DUSTER.EComm.Services.Modules.AlertEngine.Models
{
    [Table("tbl_email_queue")]
    public class EmailQueue : BaseEntity
    {
        [Key]
        public long queue_id { get; set; }
        public string? event_code { get; set; }
        public string? to_email { get; set; }
        public string? to_name { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "jsonb")]
        public string? payload_json { get; set; }
        [Computed]
        public object? payload_json_obj { get; set; }

        //Pending, Processing, Sent, Failed
        public string? status { get; set; }
        public string? error_message { get; set; }
        public int retry_count { get; set; }
        public int max_retries { get; set; }
    }

    public class EmailQueueValidator : AbstractValidator<EmailQueue>
    {
        public EmailQueueValidator()
        {
            RuleFor(x => x.event_code).NotEmpty();
            RuleFor(x => x.to_email).NotEmpty().EmailAddress();
            RuleFor(x => x.payload_json).NotEmpty();
            RuleFor(x => x.status).NotEmpty();
        }
    }
}
