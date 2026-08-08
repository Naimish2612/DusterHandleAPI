using System.Data;

namespace DUSTER.EComm.Data
{
    public interface IUnitOfWork
    {
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }

        void BeginTransaction();
        void Commit();
        void Dispose();
        void Rollback();
    }
}
