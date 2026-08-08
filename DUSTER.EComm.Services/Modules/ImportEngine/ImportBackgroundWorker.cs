using DUSTER.EComm.Data;
using DUSTER.EComm.Services.Modules.ImportEngine.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DUSTER.EComm.Services.Modules.ImportEngine
{
    public class ImportBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ImportBackgroundWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Import Background Worker Started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessNextJobAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}, Background worker encountered a fatal error.");
                }

                // Poll database every 5 seconds
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessNextJobAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var jobRepo = scope.ServiceProvider.GetRequiredService<IEIPLRepository<ImportJob>>();

            //Find a "Queued" Job
            var pendingJobs = await jobRepo.QueryAsync<ImportJob>("SELECT * FROM tbl_import_job WHERE status = 'Queued' ORDER BY job_id ASC LIMIT 1");
            var job = pendingJobs.FirstOrDefault();

            if (job == null)
                return;

            //Mark as Processing
            job.status = "Processing";
            await jobRepo.UpdateAsync(job);

            try
            {
                // DYNAMIC STRATEGY RESOLUTION
                // We fetch all registered handlers and pick the one matching our EntityType.
                var handlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IImportHandler>>();
                var handler = handlers.FirstOrDefault(h => h.EntityType == job.entity_type);

                if (handler == null)
                {
                    throw new Exception($"No import handler registered for Entity Type: {job.entity_type}");
                }

                //Execute the specific strategy generically
                await handler.ProcessAsync(job, scope);
            }
            catch (Exception ex)
            {
                job.status = "Failed";
                job.error_file_path = $"Fatal Error: {ex.Message}";
                await jobRepo.UpdateAsync(job);
            }
        }
    }
}
