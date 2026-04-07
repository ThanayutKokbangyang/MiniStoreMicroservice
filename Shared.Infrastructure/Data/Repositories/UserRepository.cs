using Dapper;
using Microsoft.Extensions.Logging;
using Shared.Common.Interfaces.Repositories;
using Shared.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Data.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        protected override string TableName => "Users";

        public UserRepository(IDbConnectionFactory connectionFactory, ILogger<UserRepository> logger) : base(connectionFactory, logger) { }

        protected override IEnumerable<string> GetAllowedSortColumns()
     => new[] { "Id", "Username", "Email", "CreatedAt", "Role" };

        public override async Task<int> CreateAsync(User entity)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, Role, CreatedBy)
                        VALUES (@Username, @Email, @PasswordHash, @PasswordSalt, @Role, @CreatedBy);
                        SELECT CAST(SCOPE_IDENTITY() as int);";
            return await connection.ExecuteScalarAsync<int>(sql, entity);
        }

        public override async Task<bool> UpdateAsync(User entity)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"UPDATE Users SET 
                        Username = @Username, Email = @Email, Role = @Role,
                        UpdatedAt = GETUTCDATE(), UpdatedBy = @UpdatedBy
                        WHERE Id = @Id AND IsActive = 1";
            var affected = await connection.ExecuteAsync(sql, entity);
            return affected > 0;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM  Users WHERE Username = @Username AND IsActive = 1", new { Username = username });
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Email = @Email AND IsActive = 1", new {Email = email});
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE RefreshToken = @RefreshToken AND IsActive = 1", new { RefreshToken = refreshToken });
        }

        public async Task<bool> UpdateRefreshTokenAsync(int UserId, string refreshToken, DateTime expiryTime)
        {
            using var connection = _connectionFactory.CreateConnection();
            var affected = await connection.ExecuteAsync(
                @"UPDATE Users SET RefreshToken = @RefreshToken, RefreshTokenExpiryTime = @ExpiryTime, UpdatedAt = GETUTCDATE() WHERE Id = @UserId", new { UserId = UserId, RefreshToken = refreshToken, ExpiryTime = expiryTime });
            return affected > 0;
        }

        public async Task<bool> IncrementFailedLoginAsync(int userId)
        {
            using var conection = _connectionFactory.CreateConnection();
            var affected = await conection.ExecuteAsync("UPDATE Users SET FailedLoginAttempts = FailedLoginAttempts + 1 WHERE Id = @UserId",
                new { UserId = userId });
            return affected > 0;
        }

        public async Task<bool> ResetFailedLoginAsync(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var affected = await connection.ExecuteAsync(
                "UPDATE Users SET FailedLoginAttempts = 0, LockoutEnd = NULL WHERE Id = @UserId", new { UserId = userId });
            return affected > 0;
        }

        public async Task<bool> LockUserAsync(int userId, DateTime lockoutEnd)
        {
            using var connection = _connectionFactory.CreateConnection();
            var affected = await connection.ExecuteAsync("UPDATE Users SET LockoutEnd = @LockoutEnd WHERE Id = @UserId",
                new { UserId = userId, LockoutEnd = lockoutEnd });
            return affected > 0;
        }
    }
}
