using Shared.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByRefreshTokenAsync(string refresh);
        Task<bool> UpdateRefreshTokenAsync(int userId, string refreshToken, DateTime expiryTime);
        Task<bool> IncrementFailedLoginAsync(int userId);
        Task<bool> ResetFailedLoginAsync(int userId);
        Task<bool> LockUserAsync(int userId, DateTime lockoutEnd);
    }
}
