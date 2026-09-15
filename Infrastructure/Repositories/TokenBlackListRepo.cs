using Common.YourProject.Models;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Persistence;
using Dapper;

namespace Infrastructure.Repositories
{
    public class TokenBlackListRepo(IDapperContext dbcontext) : ITokenBlackListRepo
    {
        public async Task<ServiceResponse<string>> AddTokenToBlackList(string TokenId, DateTime expiryDate)
        {
            using var connection = dbcontext.CreateConnection();
            // Column is Expiry, not ExpiryDate — see CreateAllTables.Postgres.sql. The
            // mismatch made every insert fail with 42703, so no token was ever blacklisted.
            const string sql = "INSERT INTO TokenBlacklist (TokenId, Expiry) VALUES (@TokenId, @Expiry)";
            try
            {
               var numRowsAffected= await connection.ExecuteAsync(sql, new { TokenId, Expiry = expiryDate });
               if (numRowsAffected > 0)  return ServiceResponse<string>.Ok("Insert Successfull");
               else return ServiceResponse<string>.Fail("No rows updated. Please check data");
            }
            catch (Exception ex) { 
                return  ServiceResponse<string>.Fail(ex.Message);
            }
        }
    }
}
