namespace DUSTER.EComm.Services.Modules.ImportEngine.Models
{
    public interface IImportStrategy<T> where T : BaseEntity
    {
        T Map(Dictionary<string, string> row);
    }
}
