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
    public class SecurityLogEntry
    {
        public string EventType { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? Details { get; set; }
        public string Severity { get; set; } = "Info";
    }
}
