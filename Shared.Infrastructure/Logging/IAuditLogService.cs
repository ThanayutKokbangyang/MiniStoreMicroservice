using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Logging
{
    public interface IAuditLogService
    {
        Task LogAsync(AuditLogEntry entry);
        Task LogSecurityEventAsync(SecurityLogEntry entry);
    }
}
