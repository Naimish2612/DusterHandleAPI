using DUSTER.EComm.Data.Helpers.Pagination;
using System;

namespace DUSTER.EComm.Services.Modules.Support.Models
{
    [Table("tbl_support_tickets")]
    public class SupportTicket : BaseEntity
    {
        [Key]
        public int ticket_id { get; set; }
        public string? ticket_no { get; set; }
        public string? order_no { get; set; }
        public long? product_code { get; set; }
        public string? user_id { get; set; }

        // Support category can be something like 'Order Issue', 'Product Inquiry', 'Payment Problem','Damaged Product', 'Wrong Item'

        public string? category { get; set; }

        // Status can be 'Open', 'In Progress', 'Resolved', 'Closed'
        public string? status { get; set; } = "Open";

        // Priority can be 'Low', 'Medium', 'High'
        public string? priority { get; set; } = "Medium";

        // Virtual properties handling Request DTO needs natively
        [Computed]
        public string? initial_message { get; set; }
        [Computed]
        public List<string>? upload_attachments { get; set; }
        [Computed]
        public string? user_name { get; set; }
        [Computed]
        public string? product_name { get; set; }
        [Computed]
        public string? mobile_no { get; set; }
        [Computed]
        public string? email_id { get; set; }
        [Computed]
        public string? name { get; set; }
    }

    [Table("tbl_support_messages")]
    public class SupportMessage : BaseEntity
    {
        [Key]
        public long message_id { get; set; }
        public int ticket_id { get; set; }

        // Sender type can be 'Customer', 'Support_Agent', 'System_Bot'
        public string? sender_type { get; set; }
        public string? sender_id { get; set; }
        public string? message_text { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "jsonb")]
        public string? attachments { get; set; }

        // Request helper
        [Computed]
        public List<string>? upload_attachments { get; set; }
    }

    public class SupportTicketValidator : AbstractValidator<SupportTicket>
    {
        public SupportTicketValidator()
        {
            RuleFor(x => x.order_no).NotEmpty().WithMessage("Order No is required.");
            RuleFor(x=>x.ticket_no).NotEmpty().WithMessage("Ticket No is required.");
            RuleFor(x => x.category).NotEmpty().WithMessage("Support Category is required.");
            // Validating the NotMapped property for the initial ticket creation
            RuleFor(x => x.initial_message).NotEmpty().When(x => x.ticket_id == 0)
                .WithMessage("An initial message is required to open a support ticket.");
        }
    }

    public class SupportMessageValidator : AbstractValidator<SupportMessage>
    {
        public SupportMessageValidator()
        {
            RuleFor(x => x.ticket_id).GreaterThan(0).WithMessage("Valid Ticket ID is required.");
            RuleFor(x => x.message_text).NotEmpty().WithMessage("Message text cannot be empty.");
        }
    }

    public class SupportTicketFilter : PaginationParams
    {
        public DateTime? from_date { get; set; }
        public DateTime? to_date { get; set; }
        public string? priority { get; set; }
        public string? status { get; set; }
        public string? ticket_no { get; set; }
        public string? order_no { get; set; }
    }
}

