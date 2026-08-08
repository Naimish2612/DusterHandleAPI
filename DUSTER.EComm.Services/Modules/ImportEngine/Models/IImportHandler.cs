using Microsoft.Extensions.DependencyInjection;

namespace DUSTER.EComm.Services.Modules.ImportEngine.Models
{
    public interface IImportHandler
    {
        string EntityType { get; }
        Task ProcessAsync(ImportJob job, IServiceScope scope);
    }
}
