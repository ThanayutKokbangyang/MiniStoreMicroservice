using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Exceptions
{
    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "Access forbidden") : base(message, 403, "FORBIDDEN") { }
    }
}
