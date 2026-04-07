using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Security
{
    public class PasswordHasher
    {
        // Work factor 12 = ~250ms per hash - ช้าพอที่จะป้องกัน brute force
        private const int WorkFactor = 12;

        public static (string hash, string salt) HashPassword(string password)
        {
            var salt = BCrypt.Net.BCrypt.GenerateSalt(WorkFactor);
            var hash = BCrypt.Net.BCrypt.HashPassword(password, salt);
            return (hash, salt);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
