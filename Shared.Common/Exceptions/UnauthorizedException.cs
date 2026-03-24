using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Exceptions
{
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message = "Unauthorized access") : base(message, 401, "UNAUTHORIZED") { }
    }
}
