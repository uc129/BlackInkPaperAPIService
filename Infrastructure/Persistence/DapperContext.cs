using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Infrastructure.Persistence
{
    public interface IDapperContext
    {
        IDbConnection CreateConnection(string name = "DefaultConnection");
    }

    public class DapperContext(IConfiguration configuration) : IDapperContext
    {
        public IDbConnection CreateConnection(string name = "DefaultConnection")
        {
            var connectionString = configuration.GetConnectionString(name);
            return new NpgsqlConnection(connectionString);
        }
    }
}
