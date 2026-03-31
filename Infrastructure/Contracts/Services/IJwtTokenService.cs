using Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.Services
{
    public interface IJwtTokenService
    {
        string GenerateToken(AppIdentityUser user, IList<string> roles);
    }
}
