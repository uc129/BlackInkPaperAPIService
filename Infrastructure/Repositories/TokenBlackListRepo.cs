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
            const string sql = "INSERT INTO TokenBlacklist (TokenId, ExpiryDate) VALUES (@TokenId, @ExpiryDate)";
            try
            {
               var numRowsAffected= await connection.ExecuteAsync(sql, new { TokenId, ExpiryDate = expiryDate });
               if (numRowsAffected > 0)  return ServiceResponse<string>.Ok("Insert Successfull");
               else return ServiceResponse<string>.Fail("No rows updated. Please check data");
            }
            catch (Exception ex) { 
                return  ServiceResponse<string>.Fail(ex.Message);
            }
        }
    }
}
