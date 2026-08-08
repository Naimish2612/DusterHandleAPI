using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.AlertEngine.Models;
using DUSTER.EComm.Services.Modules.Auth.Models;
using Newtonsoft.Json;
using Scriban;
using Scriban.Runtime;
using System.Diagnostics.Tracing;
using System.Text.Json;

namespace DUSTER.EComm.Services.Modules.AlertEngine
{
    public class AlertEngineService : IAlertEngineService
    {
        private readonly IEIPLRepository<EmailQueue> _queueRepo;
        private readonly IEIPLRepository<EmailTemplate> _templateRepo;
        private readonly ICurrentUserService _currentUserService;

        public AlertEngineService(IEIPLRepository<EmailQueue> queueRepo, IEIPLRepository<EmailTemplate> templateRepo, ICurrentUserService currentUserService)
        {
            _queueRepo = queueRepo;
            _templateRepo = templateRepo;
            _currentUserService = currentUserService;
        }

        public async Task<string> RenderTemplate(string templateContent, string jsonPayload)
        {
            // Parses unstructured JSON into a dynamic ExpandoObject
            //var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            //var dataDictionary = JsonSerializer.Deserialize<ExpandoObject>(jsonPayload, options);

            //// Scriban uses ScriptObject to safely evaluate dynamic dictionary/expando properties at runtime
            //var scriptObject = new ScriptObject();
            //scriptObject.Import(dataDictionary);

            //var context = new TemplateContext();
            //context.PushGlobal(scriptObject);

            //var template = Template.Parse(templateContent);
            //if (template.HasErrors)
            //{
            //    throw new Exception($"Template parsing error: {string.Join(", ", template.Messages)}");
            //}

            //return await template.RenderAsync(context);

            // Newtonsoft parses directly to native C# types (string, int), avoiding the JsonElement issue
            var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonPayload);

            var scriptObject = new ScriptObject();
            if (dataDictionary != null)
            {
                scriptObject.Import(dataDictionary); // Now imports true strings
            }

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            var template = Template.Parse(templateContent);
            if (template.HasErrors)
            {
                throw new Exception($"Template parsing error: {string.Join(", ", template.Messages)}");
            }

            return await template.RenderAsync(context);
        }

        public async Task<IActionResult> QueueEventNotificationAsync(string eventCode, string toEmail, string toName, object dynamicPayload)
        {
            try
            {
                // 1. Verify Event Template exists before queueing
                var templates = await _templateRepo.QueryAsync<EmailTemplate>($"SELECT * FROM tbl_email_template WHERE event_code = '{eventCode}' AND is_active = true");

                if (templates == null || !templates.Any())
                    return ResponseEntity<object>.Error(null, $"No active template found for Event: {eventCode}");

                var template = templates.First();
                object hydratedPayload = HydratePayload(eventCode, dynamicPayload, template.required_template_fields);

                // 2. Construct Queue Model
                var queueModel = new EmailQueue
                {
                    event_code = eventCode,
                    to_email = toEmail,
                    to_name = toName,
                    payload_json = System.Text.Json.JsonSerializer.Serialize(hydratedPayload),
                    status = "Pending",
                    retry_count = 0,
                    max_retries = 3
                };

                // 3. Strict Validation per EIPL Standards
                var validator = await _queueRepo.ModelValidating(new ValidationModel()
                {
                    ValidateModel = new EmailQueueValidator(),
                    Model = queueModel
                });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                // 4. Set Audit (if applicable in context)
                // queueModel.created_by = _currentUserService.UserId;

                // 5. Insert
                var result = await _queueRepo.InsertAsync(queueModel);

                if (Convert.ToInt32(result) > 0)
                    return ResponseEntity<object>.Success(queueModel.queue_id, "Notification successfully queued.");

                return ResponseEntity<object>.Error(null, "Failed to queue notification.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> GetEmailQueueByIdAsync(long id)
        {
            try
            {
                var result = await _queueRepo.GetByIdAsync(id);

                if (result != null)
                    return ResponseEntity<object>.Success(result, "Email Queue record retrieved successfully.");

                return ResponseEntity<object>.Error(null, "Email Queue record not found.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> EmailQueueListAsync(string? status = null)
        {
            try
            {
                string query = "SELECT * FROM tbl_email_queue";

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query += $" WHERE status = '{status}'";
                }

                query += " ORDER BY queue_id DESC";

                var result = await _queueRepo.QueryAsync<EmailQueue>(query);
                var reponse = result.Select(u => new
                    {
                        queue_id = u.queue_id,
                        event_code = u.event_code,
                        to_email = u.to_email,
                        to_name = u.to_name,
                        status = u.status,
                        status_message = u.error_message,
                        retry_count = u.retry_count,
                        max_retries = u.max_retries
                    }
                );

                if (result.Any())
                    return ResponseEntity<object>.Success(reponse, "Email Queue records retrieved successfully.");

                return ResponseEntity<object>.Error(null, "No Email Queue records found.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        private object HydratePayload(string eventCode, object dynamicPayload, string requiredTemplateFields)
        {
            if (dynamicPayload == null) return null;

            // Deserialize required fields
            List<string> requiredFields = null;
            if (!string.IsNullOrEmpty(requiredTemplateFields))
            {
                try
                {
                    requiredFields = System.Text.Json.JsonSerializer.Deserialize<List<string>>(requiredTemplateFields);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AlertEngineService] Error deserializing required_template_fields: {ex.Message}");
                }
            }

            // If the payload is a JsonElement (from Web API JSON deserialization)
            if (dynamicPayload is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    try
                    {
                        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
                        if (dict != null)
                        {
                            dynamicPayload = dict;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AlertEngineService] Error deserializing JsonElement payload: {ex.Message}");
                    }
                }
            }

            // If we have a dictionary payload, we can extract from it or return it
            if (dynamicPayload is IDictionary<string, object> dictPayload)
            {
                if (requiredFields == null || requiredFields.Count == 0)
                {
                    return dictPayload;
                }
                var filtered = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in requiredFields)
                {
                    if (dictPayload.TryGetValue(field, out var val))
                    {
                        filtered[field] = val;
                    }
                    else
                    {
                        filtered[field] = "";
                    }
                }
                return filtered;
            }

            // Otherwise, we treat it as an object model and map via Reflection
            var type = dynamicPayload.GetType();
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // If no fields are specified, extract all properties from the model
            var fieldsToMap = (requiredFields != null && requiredFields.Count > 0) 
                ? requiredFields 
                : properties.Select(p => p.Name).ToList();

            var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in fieldsToMap)
            {
                // Find property on object matching field name (case-insensitive)
                var prop = properties.FirstOrDefault(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    payload[field] = prop.GetValue(dynamicPayload) ?? "";
                }
                else
                {
                    // Alias / Compatibility mapping checks:
                    string aliasValue = null;

                    if (aliasValue != null)
                    {
                        payload[field] = aliasValue;
                    }
                    else
                    {
                        payload[field] = "";
                    }
                }
            }

            return payload;
        }
    }
}
