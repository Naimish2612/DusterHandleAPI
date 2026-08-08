using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Services.Modules.PrimaryDocuments.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.PrimaryDocuments
{
    public class PrimaryDocumentService : IPrimaryDocumentService
    {
        private readonly IEIPLRepository<PrimaryDocument> _primaryDocRepo;

        public PrimaryDocumentService(IEIPLRepository<PrimaryDocument> primaryDocRepo)
        {
            _primaryDocRepo = primaryDocRepo;
        }

        public async Task<IActionResult> CreatePrimaryDocumentAsync(PrimaryDocument model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Invalid document data");

                var validator = await _primaryDocRepo.ModelValidating(new ValidationModel { ValidateModel = new PrimaryDocumentValidator(), Model = model });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var checkParam = new DynamicParameters();
                checkParam.Add("DocumentName", model.document_name);
                var existing = await _primaryDocRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_primary_documents WHERE document_name = @DocumentName AND is_active = true", checkParam);
                if (existing != null)
                    return ResponseEntity<object>.Error(null, $"A primary document with the name '{model.document_name}' already exists.");

                if (model.is_active == null)
                    model.is_active = true;

                var result = await _primaryDocRepo.InsertAsync(model);

                if (result != null)
                    return ResponseEntity<object>.Success(result, "Primary Document created successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to create Primary Document.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> UpdatePrimaryDocumentAsync(PrimaryDocument model)
        {
            try
            {
                if (model == null || model.document_id <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid document data");

                var existing = await _primaryDocRepo.GetByIdAsync(model.document_id);
                if (existing == null)
                    return ResponseEntity<object>.Error(null, "Primary Document not found.");

                var validator = await _primaryDocRepo.ModelValidating(new ValidationModel { ValidateModel = new PrimaryDocumentValidator(), Model = model });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                if (existing.document_name != model.document_name)
                {
                    var checkParam = new DynamicParameters();
                    checkParam.Add("DocumentName", model.document_name);
                    checkParam.Add("DocumentId", model.document_id);
                    var nameConflict = await _primaryDocRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_primary_documents WHERE document_name = @DocumentName AND document_id != @DocumentId AND is_active = true", checkParam);
                    if (nameConflict != null)
                        return ResponseEntity<object>.Error(null, $"A primary document with the name '{model.document_name}' already exists.");
                }

                existing.document_name = model.document_name;
                existing.body_html = model.body_html;
                existing.is_active = model.is_active;

                var result = await _primaryDocRepo.UpdateAsync(existing);

                if (result > 0)
                    return ResponseEntity<object>.Success(null, "Primary Document updated successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to update Primary Document.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetPrimaryDocumentByIdAsync(int documentId)
        {
            try
            {
                if (documentId <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid document ID");

                var data = await _primaryDocRepo.GetByIdAsync(documentId);

                if (data != null)
                    return ResponseEntity<object>.Success(data, "Primary Document retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Primary Document not found.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetPrimaryDocumentByNameAsync(string documentName)
        {
            try
            {
                if (string.IsNullOrEmpty(documentName))
                    return ResponseEntity<object>.Error(null, "Invalid document name");

                var param = new DynamicParameters();
                param.Add("DocumentName", documentName);
                var data = await _primaryDocRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_primary_documents WHERE document_name = @DocumentName", param);

                if (data != null)
                    return ResponseEntity<object>.Success(data.body_html, "Primary Document retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Primary Document not found.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetAllPrimaryDocumentsAsync()
        {
            try
            {
                var data = await _primaryDocRepo.QueryAsync<PrimaryDocument>("SELECT * FROM tbl_primary_documents ORDER BY document_id DESC");

                if (data != null && data.Any())
                    return ResponseEntity<object>.Success(data, "Primary Documents retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No Primary Documents found.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
