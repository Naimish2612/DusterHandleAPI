namespace DUSTER.EComm.Services.Modules.ImportEngine.Models
{
    [Table("tbl_import_job")]
    public class ImportJob : BaseEntity
    {
        [Key]
        public long job_id { get; set; }
        public string? entity_type { get; set; }
        public string? file_path { get; set; }
        // Status can be Queued, Processing, Completed, CompletedWithErrors, Failed
        public string? status { get; set; } = "Queued";
        public int total_rows { get; set; }
        public int processed_rows { get; set; }
        public int success_count { get; set; }
        public int failed_count { get; set; }
        public string? error_file_path { get; set; }
    }

    public class ImportJobValidator : AbstractValidator<ImportJob>
    {
        public ImportJobValidator()
        {
            RuleFor(x => x.entity_type).NotEmpty();
            RuleFor(x => x.file_path).NotEmpty();
            RuleFor(x => x.status).NotEmpty();
        }
    }
}
