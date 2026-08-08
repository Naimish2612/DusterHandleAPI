using DUSTER.EComm.Services.Modules.Support.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Support
{
    public interface ISupportService
    {
        Task<IActionResult> GetProductFaqsAsync(long productCode, int subCategoryId, int categoryId);
        Task<IActionResult> GetUserTicketsAsync(string userId);
        Task<IActionResult> GetSupportTicketsAsync(SupportTicketFilter filter);
        Task<IActionResult> GetTicketThreadAsync(int ticketId, string userId, bool isSupportAgent = false);
        Task<IActionResult> CreateTicketAsync(IFormCollection model);
        Task<IActionResult> ReplyToTicketAsync(IFormCollection model, bool isSupportAgent = false);
        Task<IActionResult> UpdateTicketStatusAsync(int ticketId, string targetStatus, string userId, bool isSupportAgent = false);

        // NEW: Admin FAQ Management Methods
        Task<IActionResult> CreateOrUpdateFaqAsync(FaqMaster model);
        Task<IActionResult> CreateFaqMappingAsync(ProductFaqMapping model);
        Task<IActionResult> DeleteFaqMappingAsync(int mappingId);
        Task<IActionResult> GetAllFaqsAsync();
    }
}
