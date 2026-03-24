using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Exceptions
{
    public class TooManyRequestException : AppException
    {
        public TooManyRequestException(string message = "Too many requests. Please try again later.") : base(message, 429, "RATE_LIMIT_EXCEEDED") { }
    }
}
