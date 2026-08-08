using DUSTER.EComm.Services.Modules.ImportEngine;
using DUSTER.EComm.Services.Modules.ImportEngine.Models;

namespace DUSTER.EComm.API.Modules.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IImportServices _importService;

        public ImportController(IImportServices importService)
        {
            _importService = importService;
        }

        [HttpPost("process/file/upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string process_name)
        {
            return await _importService.UploadAndQueueJobAsync(file, process_name);
        }

        [HttpGet("process/file/status/{jobId}")]
        public async Task<IActionResult> GetStatus(long jobId)
        {
            return await _importService.GetJobStatusAsync(jobId);
        }

        [HttpGet("process/file/status/all")]
        public async Task<IActionResult> GetAllJobs()
        {
            return await _importService.GetAllJobsAsync();
        }

        [HttpGet("fields/{process_name}")]
        public async Task<IActionResult> GetImportFields(string process_name)
        {
            try
            {
                return (IActionResult)await _importService.GetImportFields(process_name);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("process/create")]
        public async Task<IActionResult> CreateImportFields([FromBody] ImportFieldsModel model)
        {
            try
            {
                return await _importService.CreateImportFieldsAsync(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("process/update")]
        public async Task<IActionResult> UpdateImportFields([FromBody] ImportFieldsModel model)
        {
            try
            {
                return await _importService.UpdateImportFieldsAsync(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("process/list")]
        public async Task<IActionResult> GetImportFieldsList()
        {
            try
            {
                return await _importService.GetImportFieldsListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("process/by/{import_id}")]
        public async Task<IActionResult> GetImportFieldsById(int import_id)
        {
            try
            {
                return await _importService.GetImportFieldsByIdAsync(import_id);
            }   
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpDelete("process/delete/{import_id}")]
        public async Task<IActionResult> DeleteImportFields(int import_id)
        {
            try
            {
                return await _importService.DeleteImportFieldsAsync(import_id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

}
