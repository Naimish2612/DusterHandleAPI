using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.FileHelper;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.ImportEngine.Models;
using Microsoft.AspNetCore.Http;

namespace DUSTER.EComm.Services.Modules.ImportEngine
{
    public class ImportServices : IImportServices
    {
        private readonly IEIPLRepository<ImportJob> _jobRepo;
        IEIPLRepository<ImportFieldsModel> _importRepo;
        private readonly ICurrentUserService _currentUserService;

        public ImportServices(IEIPLRepository<ImportJob> jobRepo, ICurrentUserService currentUserService, IEIPLRepository<ImportFieldsModel> importRepo)
        {
            _jobRepo = jobRepo;
            _currentUserService = currentUserService;
            _importRepo = importRepo;
        }

        public async Task<IActionResult> UploadAndQueueJobAsync(IFormFile file, string process_name)
        {
            if (file == null || file.Length == 0)
                return ResponseEntity<object>.Error(null, "Please upload a valid file.");

            try
            {
                // 1. Save Physical File Asynchronously
                
                //string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "imports", "raw");
                string uploadsFolder = FileHelper.ImportFilePath();

                FileHelper.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 2. Register Job in Database
                var job = new ImportJob
                {
                    entity_type = process_name,
                    file_path = filePath,
                    status = "Queued"
                };

                var validator = await _jobRepo.ModelValidating(new ValidationModel() { ValidateModel = new ImportJobValidator(), Model = job });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var result = await _jobRepo.InsertAsync(job);

                if (Convert.ToInt64(result) > 0)
                    return ResponseEntity<object>.Success("File uploaded successfully. Processing in background.");

                return ResponseEntity<object>.Error(null, "Failed to queue import job.");
            }
            catch
            {
                throw;
            }
        }

        public async Task<IActionResult> GetJobStatusAsync(long jobId)
        {
            try
            {
                var job = await _jobRepo.GetByIdAsync(jobId);
                if (job == null)
                    return ResponseEntity<object>.Error(null, "Import Job data not found.");

                return ResponseEntity<object>.Success(job, "Job status retrieved.");
            }
            catch
            {
                throw;
            }
        }

        public async Task<IActionResult> GetAllJobsAsync()
        {
            try
            {
                long user_code = _currentUserService.User.user_code;

                var param = new DynamicParameters();
                string sqlQuery = @"SELECT * FROM tbl_import_job WHERE 1=1";

                if (_currentUserService.User.user_type.ToLower() != "admin")
                {
                    sqlQuery += $" AND created_by ='{user_code}' LIMIT 100 ";
                }

                sqlQuery += " ORDER BY created_at DESC LIMIT 100 ";

                var jobs = await _jobRepo.QueryAsync<ImportJob>(sqlQuery);

                if (jobs.Any())
                {
                    //string[] currentStatus = jobs.Select(j => j.status).Distinct().ToArray();
                    var jobsWithDate = jobs.Select(job => new
                    {
                        job.job_id,
                        job.entity_type,
                        job.file_path,
                        job.status,
                        job.total_rows,
                        job.processed_rows,
                        job.success_count,
                        job.failed_count,
                        job.error_file_path,
                        job.created_at
                    });

                    var groupedData = jobsWithDate.GroupBy(job => job.status)
                            .ToDictionary(
                                group => group.Key,
                                group => group.ToList()
                            );

                    return ResponseEntity<object>.Success(groupedData, "Import Jobs retrieved.");

                }
                else
                    return ResponseEntity<object>.Success(null, "Import Files not found yet.");
            }
            catch
            {
                throw;
            }
        }
        public async Task<object> GetImportFields(string process_name)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("process_name", process_name);
                var fields = await _importRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_import_process_configuration WHERE process_name = @process_name AND is_active = true AND is_delete = false", param);

                if (fields == null)
                {
                    return ResponseEntity<object>.Error(null, "Import configuration not found or inactive.");
                }

                var main_fields = new
                {
                    required_fields = fields.required_fields,
                    non_required_fields = fields.non_required_fields,
                    max_records_allowed = fields.max_records_allowed
                };

                return ResponseEntity<object>.Success(main_fields);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IActionResult> GetImportFieldsListAsync()
        {
            try
            {
                var response = await _importRepo.QueryAsync<ImportFieldsModel>("SELECT * FROM tbl_import_process_configuration WHERE is_delete = false ORDER BY import_id DESC");

                if (response == null || !response.Any())
                    return ResponseEntity<object>.Error(null, "No import configurations found.");
                else
                    return ResponseEntity<object>.Success(response, "Import configurations retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IActionResult> GetImportFieldsByIdAsync(int import_id)
        {
            try
            {
                if (import_id <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid import ID.");

                var response = await _importRepo.GetByIdAsync(import_id);

                if (response == null || response.is_delete)
                    return ResponseEntity<object>.Error(null, "Import configuration not found.", System.Net.HttpStatusCode.NotFound);
                else
                    return ResponseEntity<object>.Success(response, "Import configuration retrieved successfully.");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IActionResult> CreateImportFieldsAsync(ImportFieldsModel model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Passing object or value are null");

                var validator = await _importRepo.ModelValidating(new ValidationModel() { ValidateModel = new ImportFieldsModelValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var checkParam = new DynamicParameters();
                checkParam.Add("ProcessName", model.process_name);
                var existing = await _importRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_import_process_configuration WHERE process_name = @ProcessName AND is_delete = false", checkParam);
                if (existing != null)
                    return ResponseEntity<object>.Error(null, $"An import configuration with the name '{model.process_name}' already exists.");

                model.is_active = true;
                model.is_delete = false;

                int response = Convert.ToInt32(await _importRepo.InsertAsync(model));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Import configuration created successfully");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while creating the import configuration.");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IActionResult> UpdateImportFieldsAsync(ImportFieldsModel model)
        {
            try
            {
                if (model == null || model.import_id <= 0)
                    return ResponseEntity<object>.Error(null, "Passing object or value are null");

                var validator = await _importRepo.ModelValidating(new ValidationModel() { ValidateModel = new ImportFieldsModelValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                ImportFieldsModel existing = await _importRepo.GetByIdAsync(model.import_id);

                if (existing == null || existing.is_delete)
                    return ResponseEntity<object>.Error(null, "Import configuration not found.");

                if (existing.process_name != model.process_name)
                {
                    var checkParam = new DynamicParameters();
                    checkParam.Add("ImportName", model.process_name);
                    checkParam.Add("ImportId", model.import_id);
                    var nameConflict = await _importRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_import_process_configuration WHERE process_name = @ImportName AND import_id != @ImportId AND is_delete = false", checkParam);
                    if (nameConflict != null)
                        return ResponseEntity<object>.Error(null, $"An import configuration with the name '{model.process_name}' already exists.");
                }

                existing.process_name = model.process_name;
                existing.required_fields = model.required_fields;
                existing.non_required_fields = model.non_required_fields;
                existing.max_records_allowed = model.max_records_allowed;
                existing.is_active = model.is_active;

                int response = await _importRepo.UpdateAsync(existing);

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Import configuration updated successfully");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while updating the import configuration.");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IActionResult> DeleteImportFieldsAsync(int import_id)
        {
            try
            {
                if (import_id <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid import ID.");

                ImportFieldsModel existing = await _importRepo.GetByIdAsync(import_id);

                if (existing == null || existing.is_delete)
                    return ResponseEntity<object>.Error(null, "Import configuration not found.");

                existing.is_active = false;
                existing.is_delete = true;

                int response = await _importRepo.UpdateAsync(existing);

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Import configuration deleted successfully");
                else
                    return ResponseEntity<object>.Error(null, "An error occurred while deleting the import configuration.");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
