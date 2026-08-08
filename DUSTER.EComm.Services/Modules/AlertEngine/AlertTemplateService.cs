using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.AlertEngine.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DUSTER.EComm.Services.Modules.AlertEngine
{
    public class AlertTemplateService : IAlertTemplateService
    {
        private readonly IEIPLRepository<EmailTemplate> _repo;
        private readonly ICurrentUserService _currentUserService;

        public AlertTemplateService(IEIPLRepository<EmailTemplate> repo, ICurrentUserService currentUserService)
        {
            _repo = repo;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> AddAsync(EmailTemplate model)
        {
            try
            {
                var validator = await _repo.ModelValidating(new ValidationModel() { ValidateModel = new EmailTemplateValidator(), Model = model });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200) return errorResponse;

                if (model.required_template_fields_obj != null)
                    model.required_template_fields = JsonSerializer.Serialize(model.required_template_fields_obj);
                else
                    model.required_template_fields = null;

                var result = await _repo.InsertAsync(model);

                if (Convert.ToInt32(result) > 0)
                    return ResponseEntity<object>.Success("Template added successfully.");

                return ResponseEntity<object>.Error(null, "Failed to add Template.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> UpdateAsync(EmailTemplate model)
        {
            try
            {
                var validator = await _repo.ModelValidating(new ValidationModel() { ValidateModel = new EmailTemplateValidator(), Model = model });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200) return errorResponse;

                var existingModel = await _repo.GetByIdAsync(model.template_id);
                if (existingModel == null)
                    return ResponseEntity<object>.Error(null, "Email Template not found.");

                var result = await _repo.UpdateAsync(model);

                if (result > 0)
                    return ResponseEntity<object>.Success("Template updated successfully.");

                return ResponseEntity<object>.Error(null, "Failed to update Template.");
            }
            catch (Exception)
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
                    return ResponseEntity<object>.Success(result, "Template retrieved successfully.");

                return ResponseEntity<object>.Error(null, "Template not found.");
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
                var result = await _repo.QueryAsync<EmailTemplate>("SELECT * FROM tbl_email_template ORDER BY template_id DESC");

                return ResponseEntity<object>.Success(result, "Templates retrieved successfully.");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
