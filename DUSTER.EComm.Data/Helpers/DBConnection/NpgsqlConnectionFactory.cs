using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace DUSTER.EComm.Data.Helpers.DBConnection
{
    public class NpgsqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public NpgsqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("dev");
        }

        public IDbConnection GetConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);

            //https://stackoverflow.com/questions/54388895/how-does-dapper-execute-query-without-explicitly-opening-connection
            // Let Dapper auto-open/close the connection when needed. No conn.Open() in the factory.

            //if (connection.State != ConnectionState.Open)
            //    connection.Open();

            return connection;
        }


    }
}
