using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.Support;
using DUSTER.EComm.Services.Modules.Support.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DUSTER.EComm.API.Modules.Support
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportController : ControllerBase
    {
        private readonly ISupportService _supportService;
        private readonly ICurrentUserService _currentUserService;

        public SupportController(ISupportService supportService, ICurrentUserService currentUserService)
        {
            _supportService = supportService;
            _currentUserService = currentUserService;
        }

        #region ADMIN: FAQ MANAGEMENT

        [HttpPost("faq/manage")]
        public async Task<IActionResult> ManageFaq([FromBody] FaqMaster model)
        {
            return await _supportService.CreateOrUpdateFaqAsync(model);
        }

        [HttpPost("faq/mapping")]
        public async Task<IActionResult> CreateFaqMapping([FromBody] ProductFaqMapping model)
        {
            return await _supportService.CreateFaqMappingAsync(model);
        }

        [HttpDelete("faq/mapping/{mappingId}")]
        public async Task<IActionResult> DeleteFaqMapping(int mappingId)
        {
            return await _supportService.DeleteFaqMappingAsync(mappingId);
        }

        [HttpGet("product-faqs")]
        public async Task<IActionResult> GetProductFaqs([FromQuery] long productCode, [FromQuery] int subCategoryId, [FromQuery] int categoryId)
        {
            return await _supportService.GetProductFaqsAsync(productCode, subCategoryId, categoryId);
        }

        [HttpGet("faq/list")]
        public async Task<IActionResult> GetAllFaqs()
        {
            return await _supportService.GetAllFaqsAsync();
        }


        #endregion 

        #region Ask Support

        [HttpGet("my-tickets")]
        public async Task<IActionResult> GetUserTickets()
        {
            return await _supportService.GetUserTicketsAsync(_currentUserService.User.user_code.ToString());
        }

        [HttpPost("threads")]
        public async Task<IActionResult> GetSupportTickets([FromBody] SupportTicketFilter filter)
        {
            return await _supportService.GetSupportTicketsAsync(filter);
        }

        [HttpGet("ticket-thread/{ticketId}")]
        public async Task<IActionResult> GetTicketThread(int ticketId)
        {
            return await _supportService.GetTicketThreadAsync(ticketId, _currentUserService.User.user_code.ToString());
        }

        [HttpGet("agent/ticket-thread/{ticketId}")]
        public async Task<IActionResult> GetAgentTicketThread(int ticketId)
        {
            return await _supportService.GetTicketThreadAsync(ticketId, _currentUserService.User.user_code.ToString(), true);
        }

        [HttpPost("raise-ticket")]
        public async Task<IActionResult> RaiseSupportTicket(IFormCollection model)
        {
            return await _supportService.CreateTicketAsync(model);
        }

        [HttpPost("ticket-reply")]
        public async Task<IActionResult> ReplyToTicket(IFormCollection model)
        {
            string adminUserId = _currentUserService.User.user_code.ToString();
            return await _supportService.ReplyToTicketAsync(model, isSupportAgent: false);
        }

        [HttpPost("agent/ticket-reply")]
        public async Task<IActionResult> AdminReplyToTicket(IFormCollection model)
        {
            string adminUserId = _currentUserService.User.user_code.ToString();
            return await _supportService.ReplyToTicketAsync(model, isSupportAgent: true);
        }

        [HttpPost("ticket-status/update")]
        public async Task<IActionResult> AdminUpdateStatus([FromQuery] int ticketId, [FromQuery] string status)
        {
            string adminUserId = _currentUserService.User.user_code.ToString();
            return await _supportService.UpdateTicketStatusAsync(ticketId, status, adminUserId, isSupportAgent: true);
        }

        #endregion
    }
}
