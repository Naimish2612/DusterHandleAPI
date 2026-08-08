using System.Data;

namespace DUSTER.EComm.Data.Helpers.DBConnection
{
    public interface IDbConnectionFactory
    {
        IDbConnection GetConnection();
    }
}
