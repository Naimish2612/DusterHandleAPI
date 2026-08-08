using DUSTER.EComm.Data.Helpers.DBConnection;
using System.Data;

namespace DUSTER.EComm.Data
{
    public partial class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private IDbConnection _connection;
        private IDbTransaction _transaction;

        public UnitOfWork(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IDbConnection Connection => _connection ??= _connectionFactory.GetConnection();
        public IDbTransaction? Transaction => _transaction;

        public void BeginTransaction()
        {
            if (_transaction == null)
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                _transaction = _connection.BeginTransaction();
            }
        }

        public void Commit()
        {
            _transaction?.Commit();
            DisposeTransaction();
        }

        public void Rollback()
        {
            _transaction?.Rollback();
            DisposeTransaction();
        }

        private void DisposeTransaction()
        {
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Dispose()
        {
            DisposeTransaction();
            _connection?.Dispose();
        }
    }
}
