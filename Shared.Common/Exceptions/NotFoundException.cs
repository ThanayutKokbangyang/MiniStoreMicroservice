using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Exceptions
{
    public class NotFoundException : AppException
    {
        public NotFoundException(string entity, object id) : base($"{entity} with id '{id} was not found.'",404,"NOT_FOUND") { }
    }
}
