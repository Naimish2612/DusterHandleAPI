using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Dapper;
using DUSTER.EComm.Data.Helpers.Cloudinary;
using DUSTER.EComm.Data.Helpers.Strings;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.Catelog.Models;
using DUSTER.EComm.Services.Modules.Support;
using DUSTER.EComm.Services.Modules.Support.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;
using System.Net.Mail;
using System.Reflection;
using System.Text.Json;

namespace DUSTER.EComm.Data.Services
{
    public class SupportService : ISupportService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IEIPLRepository<SupportTicket> _ticketRepo;
        private readonly IEIPLRepository<SupportMessage> _messageRepo;
        private readonly IEIPLRepository<FaqMaster> _faqRepo;
        private readonly IEIPLRepository<ProductFaqMapping> _faqMappingRepo;
        private readonly Cloudinary _cloudinary;
        private readonly IOptions<CloudinarySettings> _cloudinarySettings;

        public SupportService(ICurrentUserService currentUser,
            IEIPLRepository<SupportTicket> ticketRepo,
            IEIPLRepository<SupportMessage> messageRepo,
            IEIPLRepository<FaqMaster> faqRepo,
            IEIPLRepository<ProductFaqMapping> faqMappingRepo,
            IOptions<CloudinarySettings> cloudinarySettings)
        {
            _ticketRepo = ticketRepo;
            _messageRepo = messageRepo;
            _faqRepo = faqRepo;
            _faqMappingRepo = faqMappingRepo;
            _currentUser = currentUser;
            _cloudinarySettings = cloudinarySettings;
            Account account = new Account(cloudinarySettings.Value.CloudName, cloudinarySettings.Value.ApiKey, cloudinarySettings.Value.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        #region FAQ

        public async Task<IActionResult> CreateOrUpdateFaqAsync(FaqMaster model)
        {
            var validator = await _faqRepo.ModelValidating(new ValidationModel() { ValidateModel = new FaqMasterValidator(), Model = model });
            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            if (model.faq_id == 0)
            {
                var result = await _faqRepo.InsertAsync(model);
                return ResponseEntity<object>.Success(result,"FAQ created successfully.");
            }
            else
            {
                await _faqRepo.UpdateAsync(model);
                return ResponseEntity<object>.Success("FAQ updated successfully.");
            }
        }

        public async Task<IActionResult> CreateFaqMappingAsync(ProductFaqMapping model)
        {
            var validator = await _faqMappingRepo.ModelValidating(new ValidationModel() { ValidateModel = new ProductFaqMappingValidator(), Model = model });
            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            // Check if this exact mapping already exists to prevent duplicates
            string checkSql = "SELECT COUNT(1) FROM tbl_product_faq_mapping WHERE faq_id = @FaqId AND target_type = @Type AND target_id = @TargetId";
            var param = new DynamicParameters();
            param.Add("FaqId", model.faq_id);
            param.Add("Type", model.target_type);
            param.Add("TargetId", model.target_id);

            var existingCount = await _faqMappingRepo.QueryAsync<int>(checkSql, param);

            if (existingCount.FirstOrDefault() > 0)
                return ResponseEntity<object>.Error(null, "This FAQ is already mapped to this target.");

            var result = await _faqMappingRepo.InsertAsync(model);

            if (result == null || Convert.ToInt32(result) <= 0)
                return ResponseEntity<object>.Error(null, "Failed to create FAQ mapping.");
            else
                return ResponseEntity<object>.Success("FAQ mapping applied successfully.");
        }

        public async Task<IActionResult> DeleteFaqMappingAsync(int mappingId)
        {
            var result = await _faqMappingRepo.DeleteAsync(mappingId);

            if (result > 0)
                return ResponseEntity<object>.Success(null, "FAQ mapping removed successfully.");

            return ResponseEntity<object>.Error(null, "Mapping not found.", HttpStatusCode.NotFound);
        }

        public async Task<IActionResult> GetProductFaqsAsync(long productCode, int subCategoryId, int categoryId)
        {
            string sql = @"
                SELECT DISTINCT fm.* FROM tbl_faq_master fm
                JOIN tbl_product_faq_mapping pfm ON fm.faq_id = pfm.faq_id
                WHERE fm.is_active = true 
                AND (
                    (pfm.target_type = 'Product' AND pfm.target_id = @ProductCode)
                    OR (pfm.target_type = 'SubCategory' AND pfm.target_id = @SubCategoryId)
                    OR (pfm.target_type = 'Category' AND pfm.target_id = @CategoryId)
                )
                ORDER BY fm.faq_id ASC";

            var param = new DynamicParameters();
            param.Add("ProductCode", productCode);
            param.Add("SubCategoryId", subCategoryId);
            param.Add("CategoryId", categoryId);


            var faqs = await _faqRepo.QueryAsync<FaqMaster>(sql, param);

            if (faqs == null || !faqs.Any())
                return ResponseEntity<object>.Error(null, "No FAQs found for this product.");

            return ResponseEntity<object>.Success(faqs);
        }

        public async Task<IActionResult> GetAllFaqsAsync()
        {
            try
            {
                string sql = @"SELECT 
                                    f.*, 
                                    CASE pm.target_type 
                                        WHEN 'Category' THEN pc.name || ' (Product Category)' 
                                        WHEN 'SubCategory' THEN psc.name || ' (Product SubCategory)' 
                                        WHEN 'Product' THEN p.name || ' (Product)' 
                                    END AS associated_field 
                                FROM tbl_faq_master f 
                                LEFT JOIN tbl_product_faq_mapping pm ON f.faq_id = pm.faq_id 
                                LEFT JOIN tbl_product_category pc ON pm.target_id = pc.category_id AND pm.target_type = 'Category' 
                                LEFT JOIN tbl_product_sub_category psc ON pm.target_id = psc.sub_category_id AND pm.target_type = 'SubCategory' 
                                LEFT JOIN tbl_products p ON pm.target_id = p.product_code AND pm.target_type = 'Product' 
                                ORDER BY f.faq_id DESC";
                var faqs = await _faqRepo.QueryAsync<FaqMaster>(sql);

                if (faqs == null || !faqs.Any())
                    return ResponseEntity<object>.Error(null, "No FAQs found.");

                return ResponseEntity<object>.Success(faqs, "FAQs retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Ask For Support

        public async Task<IActionResult> GetUserTicketsAsync(string userId)
        {
            string sql = "SELECT tbl_support_tickets.*,tbl_products.name FROM tbl_support_tickets LEFT JOIN tbl_products ON tbl_products.product_code = tbl_support_tickets.product_code WHERE user_id = @UserId ORDER BY tbl_support_tickets.created_at DESC";

            var param = new DynamicParameters();
            param.Add("UserId", userId);

            var tickets = await _ticketRepo.QueryAsync<SupportTicket>(sql, param);

            if (tickets == null || !tickets.Any())
                return ResponseEntity<object>.Success("No tickets found for this user.");

            return ResponseEntity<object>.Success(tickets);
        }

        public async Task<IActionResult> GetSupportTicketsAsync(SupportTicketFilter filter)
        {
            try
            {
                var parameters = new DynamicParameters();
                string sql = @"
                    SELECT 
                        st.*, 
                        u.user_name, 
                        u.mobile_no,
                        u.email_id,
                        p.name AS product_name 
                    FROM tbl_support_tickets st
                    LEFT JOIN tbl_users u ON CAST(u.user_code AS VARCHAR) = st.user_id
                    LEFT JOIN tbl_products p ON p.product_code = st.product_code
                    WHERE 1=1";

                if (filter.from_date.HasValue && filter.to_date.HasValue)
                {
                    sql += " AND st.created_at BETWEEN @StartDateFilter AND @EndDateFilter";
                    parameters.Add("StartDateFilter", filter.from_date.Value.Date);
                    parameters.Add("EndDateFilter", filter.to_date.Value.Date.AddDays(1).AddTicks(-1));
                }
                else if (filter.from_date.HasValue)
                {
                    sql += " AND st.created_at >= @StartDateFilter";
                    parameters.Add("StartDateFilter", filter.from_date.Value.Date);
                }
                else if (filter.to_date.HasValue)
                {
                    sql += " AND st.created_at <= @EndDateFilter";
                    parameters.Add("EndDateFilter", filter.to_date.Value.Date.AddDays(1).AddTicks(-1));
                }

                if (!string.IsNullOrEmpty(filter.priority))
                {
                    sql += " AND st.priority = @Priority";
                    parameters.Add("Priority", filter.priority);
                }

                if (!string.IsNullOrEmpty(filter.status))
                {
                    sql += " AND st.status = @Status";
                    parameters.Add("Status", filter.status);
                }

                if (!string.IsNullOrEmpty(filter.ticket_no))
                {
                    sql += " AND st.ticket_no LIKE @TicketNo";
                    parameters.Add("TicketNo", $"%{filter.ticket_no}%");
                }

                if (!string.IsNullOrEmpty(filter.order_no))
                {
                    sql += " AND st.order_no LIKE @OrderNo";
                    parameters.Add("OrderNo", $"%{filter.order_no}%");
                }

                sql += " ORDER BY st.created_at DESC";

                var tickets = await _ticketRepo.QueryAsync<SupportTicket>(sql, parameters);

                if (tickets == null || !tickets.Any())
                    return ResponseEntity<object>.Success(new List<SupportTicket>(), "No tickets found matching the criteria.");

                return ResponseEntity<object>.Success(tickets, "Support tickets retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetTicketThreadAsync(int ticketId, string userId, bool isSupportAgent = false)
        {
            // Fetch the ticket by its semantic primary key
            var ticket = await _ticketRepo.GetByIdAsync(ticketId);

            if (ticket == null)
                return ResponseEntity<object>.Error(null, "Ticket workspace not found.", HttpStatusCode.InternalServerError);

            // Security Check: If it's NOT a support agent, ensure the logged-in customer owns this ticket
            if (!isSupportAgent && ticket.user_id != userId)
                return ResponseEntity<object>.Error(null, "Unauthorized access to this ticket thread.", HttpStatusCode.InternalServerError);

            // Fetch the sequential chronological dialogue log
            string sql = "SELECT * FROM tbl_support_messages WHERE ticket_id = @TicketId ORDER BY message_id ASC";
            var parameters = new DynamicParameters();
            parameters.Add("TicketId", ticketId);

            var thread = await _messageRepo.QueryAsync<SupportMessage>(sql, parameters);

            if (thread.Any())
            {
                return ResponseEntity<object>.Success(thread.Select(x => new
                {
                    x.message_id,
                    x.ticket_id,
                    x.sender_type,
                    x.sender_id,
                    x.message_text,
                    x.created_at,
                    upload_attachments = string.IsNullOrEmpty(x.attachments) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(x.attachments)
                }).ToList(), "Ticket conversation thread retrieved successfully.");
            }
            else
                return ResponseEntity<object>.Error(null, "Ticket workspace not found.");
        }

        public async Task<IActionResult> CreateTicketAsync(IFormCollection model)
        {
            if (model == null || !model.ContainsKey("data"))
                return ResponseEntity<object>.Error(null, "Form data is missing or invalid.");

            if (model.Files.Count > 5)
                return ResponseEntity<object>.Error(null, "You can attached maximum 5 files.");

            var data = model["data"].ToString();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var st = System.Text.Json.JsonSerializer.Deserialize<SupportTicket>(data, options);

            st.ticket_no = $"TCKT-{DateTime.Now.ToString("HHmmssyyMMdd").Replace("1","E")}-{StringHelper.GetUniqueStringV2(10)}"; 
            st.user_id = _currentUser.User.user_code.ToString();
            st.status = "Open";
            
            var validator = await _ticketRepo.ModelValidating(new ValidationModel() { ValidateModel = new SupportTicketValidator(), Model = st });

            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            var ticketIdResult = await _ticketRepo.InsertAsync(st);
            int ticketId = Convert.ToInt32(ticketIdResult);

            // Insert Initial Chat Message
            var message = new SupportMessage
            {
                ticket_id = ticketId,
                sender_type = "Customer",
                sender_id = st.user_id,
                message_text = st.initial_message!
            };

            if (model.Files.Any())
            {
                var attachmentResponse = (await UploadAttachmentFiles(model.Files)).ToList();

                if (attachmentResponse == null || attachmentResponse.Any())
                {
                    message.attachments = attachmentResponse != null ? JsonConvert.SerializeObject(attachmentResponse) : null;
                }
            }

            var response = await _messageRepo.InsertAsync(message);

            if (response == null || Convert.ToInt32(response) <= 0)
                return ResponseEntity<object>.Error(null, "Failed to create initial message for the ticket.");

            return ResponseEntity<object>.Success("Ticket submitted successfully");
        }

        public async Task<IActionResult> ReplyToTicketAsync(IFormCollection model, bool isSupportAgent = false)
        {
            if (model == null || !model.ContainsKey("data"))
                return ResponseEntity<object>.Error(null, "Form data is missing or invalid.");

            if (model.Files.Count > 5)
                return ResponseEntity<object>.Error(null, "You can attached maximum 5 files.");

            var data = model["data"].ToString();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var st = System.Text.Json.JsonSerializer.Deserialize<SupportMessage>(data, options);


            var validator = await _messageRepo.ModelValidating(new ValidationModel() { ValidateModel = new SupportMessageValidator(), Model = st });
            if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                return errorResponse;

            var ticket = await _ticketRepo.GetByIdAsync(st.ticket_id);
            if (ticket == null)
                return ResponseEntity<object>.Error(null, "Ticket workspace not found", HttpStatusCode.InternalServerError);

            // Block any further customer threads if the ticket is permanently closed
            if (!isSupportAgent && ticket.status == "Closed")
                return ResponseEntity<object>.Error(null, "This support ticket is closed. Please open a new ticket for any further issues.", HttpStatusCode.InternalServerError);

            string userId = _currentUser.User.user_code.ToString();

            if (!isSupportAgent && ticket.user_id != userId)
                return ResponseEntity<object>.Error(null, "Unauthorized access to this ticket", HttpStatusCode.InternalServerError);

            st.sender_type = isSupportAgent ? "Support_Agent" : "Customer";
            st.sender_id = userId;

            if (model.Files.Any())
            {
                var attachmentResponse = (await UploadAttachmentFiles(model.Files)).ToList();

                if (attachmentResponse == null || attachmentResponse.Any())
                {
                    st.attachments = attachmentResponse != null ? JsonConvert.SerializeObject(attachmentResponse) : null;
                }
            }

            await _messageRepo.InsertAsync(st);

            if (isSupportAgent)
            {
                // If an agent replies, move status to Under Investigation (or keep it as is)
                if (ticket.status == "Open")
                {
                    ticket.status = "In Progress";
                    await _ticketRepo.UpdateAsync(ticket);
                }
            }
            else
            {
                // If a customer replies to a closed/resolved ticket, auto-reopen it
                if (ticket.status == "Resolved" || ticket.status == "Closed")
                {
                    ticket.status = "Open";
                    await _ticketRepo.UpdateAsync(ticket);
                }
            }

            return ResponseEntity<object>.Success(null, "Response appended successfully");
        }

        public async Task<IActionResult> UpdateTicketStatusAsync(int ticketId, string targetStatus, string userId, bool isSupportAgent = false)
        {
            //Enforce allowed target status parameters
            var allowedStatuses = new[] { "Resolved", "Closed" };

            if (!allowedStatuses.Contains(targetStatus))
            {
                return ResponseEntity<object>.Error(null, "Invalid status transition targeted.", HttpStatusCode.InternalServerError);
            }

            // Fetch the Ticket entity
            var ticket = await _ticketRepo.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                return ResponseEntity<object>.Error(null, "Ticket workspace not found.", HttpStatusCode.InternalServerError);
            }

            // Security: Customers can only update status on their own tickets
            if (!isSupportAgent && ticket.user_id != userId)
            {
                return ResponseEntity<object>.Error(null, "Unauthorized operation.", HttpStatusCode.InternalServerError);
            }

            // Prevent modifying an already finalized/closed ticket
            if (ticket.status == "Closed")
            {
                return ResponseEntity<object>.Error(null, "This ticket is permanently closed and cannot be modified.", HttpStatusCode.InternalServerError);
            }

            // Update Status
            ticket.status = targetStatus;
            await _ticketRepo.UpdateAsync(ticket);

            // Append an automated system message log to the thread for transparency
            var systemLog = new SupportMessage
            {
                ticket_id = ticketId,
                sender_type = "System_Bot",
                sender_id = "SYSTEM",
                message_text = isSupportAgent
                    ? $"Ticket status has been updated to '{targetStatus}' by Support Administration."
                    : $"Ticket has been explicitly marked as '{targetStatus}' by the customer."
            };

            var response = await _messageRepo.InsertAsync(systemLog);

            if (Convert.ToInt64(response) > 0)
                return ResponseEntity<object>.Success(null, $"Ticket status successfully updated to {targetStatus}.");
            else
                return ResponseEntity<object>.Error(null, $"Something error from server side.");
        }

        public async Task<List<string>> UploadAttachmentFiles(IFormFileCollection files)
        {
            try
            {

                List<string> imagePath = new List<string>();

                string[] array = { "jpg", "jpeg", "png" };
                for (int i = 0; i < files.Count; i++)
                {
                    IFormFile _file;
                    _file = files[i];

                    using var stream = _file.OpenReadStream();

                    string fileExtension = Path.GetExtension(_file.FileName);

                    if (array.Contains(fileExtension))
                        return new List<string>();

                    string file_name = $"{_currentUser.User.user_code}_{StringHelper.GetUniqueString(10)}{Path.GetExtension(files[i].FileName)}";
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file_name, stream),
                        Folder = $"EComm/Support/{_currentUser.User.user_code}",// the specific folder you want in Cloudinary
                        UseFilename = true,                   // use original filename
                        UniqueFilename = true,                // let Cloudinary add uniqueness
                        Overwrite = false,                    // do not overwrite existing
                                                              //ResourceType = ResourceType.Image
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK && uploadResult.StatusCode != System.Net.HttpStatusCode.Created)
                    {
                        continue;
                    }

                    // public url (https)
                    var publicUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Uri?.ToString();

                    if (publicUrl != null)
                        imagePath.Add(publicUrl);
                }

                return imagePath;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}