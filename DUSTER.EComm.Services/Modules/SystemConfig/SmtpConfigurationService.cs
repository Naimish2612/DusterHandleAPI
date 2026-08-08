using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.Services.DropdownServices.Models;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.SystemConfig.Models;

namespace DUSTER.EComm.Services.Modules.SystemConfig
{
    public class SmtpConfigurationService : ISmtpConfigurationService
    {
        private readonly IEIPLRepository<SmtpConfiguration> _repo;
        private readonly ICurrentUserService _currentUserService;

        public SmtpConfigurationService(IEIPLRepository<SmtpConfiguration> repo, ICurrentUserService currentUserService)
        {
            _repo = repo;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> AddAsync(SmtpConfiguration model)
        {
            try
            {
                var validator = await _repo.ModelValidating(new ValidationModel() { ValidateModel = new SmtpConfigurationValidator(), Model = model });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200) return errorResponse;

                var result = await _repo.InsertAsync(model);
                if (Convert.ToInt32(result) > 0)
                    return ResponseEntity<object>.Success(model.smtp_config_id, "SMTP Configuration added successfully.");

                return ResponseEntity<object>.Error(null, "Failed to add SMTP Configuration.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> UpdateAsync(SmtpConfiguration model)
        {
            try
            {
                var validator = await _repo.ModelValidating(new ValidationModel() { ValidateModel = new SmtpConfigurationValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200) return errorResponse;

                var existingModel = await _repo.GetByIdAsync(model.smtp_config_id);
                if (existingModel == null)
                    return ResponseEntity<object>.Error(null, "SMTP Configuration not found.");

                var result = await _repo.UpdateAsync(model);

                if (result > 0)
                    return ResponseEntity<object>.Success("SMTP Configuration updated successfully.");

                return ResponseEntity<object>.Error(null, "Failed to update SMTP Configuration.");
            }
            catch(Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> GetByIdAsync(int id)
        {
            try
            {
                var result = await _repo.GetByIdAsync(id);

                if (result != null)
                    return ResponseEntity<object>.Success(result, "SMTP Configuration retrieved successfully.");

                return ResponseEntity<object>.Error(null, "SMTP Configuration not found.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> ListAsync()
        {
            try
            {
                var result = await _repo.QueryAsync<SmtpConfiguration>("SELECT * FROM tbl_smtp_configuration ORDER BY smtp_config_id DESC");
                return ResponseEntity<object>.Success(result, "SMTP Configurations retrieved successfully.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> GetSmtpCategoryDropdownAsync()
        {
            try
            {
                var result = await _repo.GetDropdownAsync(new DropdownRequestModel
                {
                    table_name = "tbl_smtp_configuration",
                    table_columns = "DISTINCT smtp_category as id, smtp_category as value",
                    StaticFilters = new Dictionary<string, object> { { "is_active", true } }
                });
                return ResponseEntity<object>.Success(result, "SMTP Categories retrieved successfully.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> GetEmailDropdownByCategoryAsync(string smtpCategory)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(smtpCategory))
                {
                    return ResponseEntity<object>.Error(null, "SMTP Category is required.");
                }

                var filters = new Dictionary<string, object>
                {
                    { "smtp_category", smtpCategory.Trim() },
                    { "is_active", true }
                };

                var result = await _repo.GetDropdownAsync(new DropdownRequestModel
                {
                    table_name = "tbl_smtp_configuration",
                    table_columns = "smtp_config_id as id, from_email as value",
                    StaticFilters = filters
                });
                return ResponseEntity<object>.Success(result, "SMTP Emails retrieved successfully.");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
