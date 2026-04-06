using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Logging
{
    public class AuditLogEntry
    {
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public object? OldValues { get; set; }
        public object? NewValues { get; set; }
        public string ServiceName { get; set; } = string.Empty;
    }
}
