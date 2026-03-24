using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Exceptions
{
    public class ValidationException : AppException
    {
        public List<string> ValidationError { get; }
        public ValidationException(List<string> errors) : base("One or more validation errors occurred.", 422, "VALIDATION_ERROR")
        {
            ValidationError = errors;
        }
    }
}
