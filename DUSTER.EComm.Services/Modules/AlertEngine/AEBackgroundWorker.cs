using DUSTER.EComm.Data;
using DUSTER.EComm.Services.Modules.AlertEngine.Models;
using DUSTER.EComm.Services.Modules.SystemConfig;
using DUSTER.EComm.Services.Modules.SystemConfig.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DUSTER.EComm.Services.Modules.AlertEngine
{
    public class AEBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AEBackgroundWorker> _logger;
        private readonly int _batchSize = 20;
        private readonly int _maxDegreeOfParallelism = 5; // Processes 5 emails simultaneously

        public AEBackgroundWorker(IServiceProvider serviceProvider, ILogger<AEBackgroundWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Alert Engine Worker Started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessEmailBatchAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fatal error in Alert Engine Worker loop.");
                }

                // Poll every 10 seconds
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessEmailBatchAsync(CancellationToken cancellationToken)
        {
            List<EmailQueue> pendingEmails;

            // 1. Fetch Batch inside its own scope
            using (var scope = _serviceProvider.CreateScope())
            {
                var queueRepo = scope.ServiceProvider.GetRequiredService<IEIPLRepository<EmailQueue>>();

                // Fetch Pending OR Failed (that haven't exceeded retries)
                pendingEmails = (await queueRepo.QueryAsync<EmailQueue>(
                    $@"SELECT * FROM tbl_email_queue 
                       WHERE status = 'Pending' OR (status = 'Failed' AND retry_count < max_retries)
                       ORDER BY queue_id ASC 
                       LIMIT {_batchSize}")).ToList();
            }

            if (!pendingEmails.Any()) return;

            _logger.LogInformation($"Processing batch of {pendingEmails.Count} emails using multithreading.");

            // 2. PARALLEL EXECUTION
            // Automatically spins up multiple threads from the ThreadPool
            await Parallel.ForEachAsync(pendingEmails, new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (emailTask, token) =>
            {
                // CRITICAL: Entity Framework Core DbContext is NOT thread-safe.
                // We MUST create a brand new Injection Scope for every single parallel thread.
                using var threadScope = _serviceProvider.CreateScope();

                var threadQueueRepo = threadScope.ServiceProvider.GetRequiredService<IEIPLRepository<EmailQueue>>();
                var threadTemplateRepo = threadScope.ServiceProvider.GetRequiredService<IEIPLRepository<EmailTemplate>>();
                var smtpConfigRepo = threadScope.ServiceProvider.GetRequiredService<IEIPLRepository<SmtpConfiguration>>();

                var templateEngine = threadScope.ServiceProvider.GetRequiredService<IAlertEngineService>();
                var smtpDelivery = threadScope.ServiceProvider.GetRequiredService<ISmtpDeliveryService>(); // MailKit wrapper

                try
                {

                    emailTask.status = "Processing";
                    await threadQueueRepo.UpdateAsync(emailTask);

                    //template
                    var templates = await threadTemplateRepo.QueryAsync<EmailTemplate>(
                        $"SELECT * FROM tbl_email_template WHERE event_code = '{emailTask.event_code}'");
                    var template = templates.FirstOrDefault();

                    if (template == null) throw new Exception("Template missing or deactivated.");

                    //Compile HTML with dynamic JSON
                    var compiledSubject = await templateEngine.RenderTemplate(template.subject_template, emailTask.payload_json);
                    var compiledBody = await templateEngine.RenderTemplate(template.body_html, emailTask.payload_json);

                    //SMTP
                    SmtpConfiguration smtpConfig = null;
                    if (template.smtp_config_id.HasValue)
                    {
                        var smtpConfigs = await smtpConfigRepo.QueryAsync<SmtpConfiguration>(
                            $"SELECT * FROM tbl_smtp_configuration WHERE smtp_config_id = {template.smtp_config_id.Value} AND is_active = true");
                        smtpConfig = smtpConfigs.FirstOrDefault();
                    }

                    if (smtpConfig == null)
                    {
                        var defaultConfigs = await smtpConfigRepo.QueryAsync<SmtpConfiguration>(
                            "SELECT * FROM tbl_smtp_configuration WHERE is_default = true AND is_active = true");
                        smtpConfig = defaultConfigs.FirstOrDefault();
                    }

                    if (smtpConfig == null) throw new Exception("No active SMTP configuration found.");

                    // Dispatch email
                    var emailSendResponse = await smtpDelivery.SendEmailAsync(smtpConfig, emailTask.to_email, emailTask.to_name, compiledSubject, compiledBody);

                    if (emailSendResponse.Contains("2.0.0 OK"))
                    {
                        emailTask.status = "Sent";
                        emailTask.error_message = emailSendResponse.ToString();
                        await threadQueueRepo.UpdateAsync(emailTask);
                    }
                    else
                    {
                        emailTask.error_message = emailSendResponse.ToString();
                        emailTask.retry_count++;
                        emailTask.status = emailTask.retry_count >= emailTask.max_retries ? "DeadLetter" : "Failed";
                        await threadQueueRepo.UpdateAsync(emailTask);
                    }
                }
                catch (Exception ex)
                {
                    // Handle Failures & Retries
                    emailTask.retry_count++;
                    emailTask.error_message = ex.Message;
                    emailTask.status = emailTask.retry_count >= emailTask.max_retries ? "DeadLetter" : "Failed";

                    await threadQueueRepo.UpdateAsync(emailTask);
                }
            });
        }
    }
}
