using DUSTER.EComm.Services.Modules.PrimaryDocuments.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.PrimaryDocuments
{
    public interface IPrimaryDocumentService
    {
        Task<IActionResult> CreatePrimaryDocumentAsync(PrimaryDocument model);
        Task<IActionResult> UpdatePrimaryDocumentAsync(PrimaryDocument model);
        Task<IActionResult> GetPrimaryDocumentByIdAsync(int documentId);
        Task<IActionResult> GetPrimaryDocumentByNameAsync(string documentName);
        Task<IActionResult> GetAllPrimaryDocumentsAsync();
    }
}
