using DUSTER.EComm.Services.Modules.ImportEngine.Models;
using Microsoft.AspNetCore.Http;

namespace DUSTER.EComm.Services.Modules.ImportEngine
{
    public interface IImportServices
    {
        Task<IActionResult> UploadAndQueueJobAsync(IFormFile file, string entityType);
        Task<IActionResult> GetJobStatusAsync(long jobId);
        Task<IActionResult> GetAllJobsAsync();

        Task<object> GetImportFields(string process_name);
        Task<IActionResult> GetImportFieldsListAsync();
        Task<IActionResult> GetImportFieldsByIdAsync(int import_id);
        Task<IActionResult> CreateImportFieldsAsync(ImportFieldsModel model);
        Task<IActionResult> UpdateImportFieldsAsync(ImportFieldsModel model);
        Task<IActionResult> DeleteImportFieldsAsync(int import_id);

    }
}
