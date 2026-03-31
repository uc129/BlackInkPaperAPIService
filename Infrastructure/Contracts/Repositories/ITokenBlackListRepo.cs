using Common.YourProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.Repositories
{
    public interface ITokenBlackListRepo
    {
        Task<ServiceResponse<string>> AddTokenToBlackList(string TokenId, DateTime expiryDate);
    }
}
