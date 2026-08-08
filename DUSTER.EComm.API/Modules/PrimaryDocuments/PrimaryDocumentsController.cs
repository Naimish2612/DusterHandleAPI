using DUSTER.EComm.Services.Modules.Masters.Models;
using DUSTER.EComm.Services.Modules.PrimaryDocuments;
using DUSTER.EComm.Services.Modules.PrimaryDocuments.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DUSTER.EComm.API.Modules.PrimaryDocuments
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrimaryDocumentsController : ControllerBase
    {
        private readonly IPrimaryDocumentService _primaryDocumentService;
        public PrimaryDocumentsController(IPrimaryDocumentService primaryDocumentService)
        {
            _primaryDocumentService = primaryDocumentService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllPrimaryDocuments()
        {
            try
            {
                return await _primaryDocumentService.GetAllPrimaryDocumentsAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("get/by/{id}")]
        public async Task<IActionResult> GetPrimaryDocumentById(int id)
        {
            try
            {
                return await _primaryDocumentService.GetPrimaryDocumentByIdAsync(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost("add")]
        public async Task<IActionResult> AddPrimaryDocuments([FromBody] PrimaryDocument model)
        {
            try
            {
                return await _primaryDocumentService.CreatePrimaryDocumentAsync(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("edit")]
        public async Task<IActionResult> EditProductCategory([FromBody] PrimaryDocument model)
        {
            try
            {
                return await _primaryDocumentService.UpdatePrimaryDocumentAsync(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpGet("get/by/document_{name}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPrimaryDocumentByName(string name)
        {
            try
            {
                return await _primaryDocumentService.GetPrimaryDocumentByNameAsync(name);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
