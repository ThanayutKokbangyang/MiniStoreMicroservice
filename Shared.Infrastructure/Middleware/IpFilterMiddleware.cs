using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Middleware
{
    // ============================================================
    // IP Whitelist/Blacklist Middleware
    // ============================================================
    public class IpFilterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpFilterMiddleware> _logger;
        private readonly HashSet<string> _blockedIps;
        public IpFilterMiddleware(RequestDelegate next, ILogger<IpFilterMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _blockedIps = new HashSet<string>(); // Load from config/database in production
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            if (clientIp != null && _blockedIps.Contains(clientIp))
            {
                _logger.LogWarning("Blocked IP attempted access: {IP}", clientIp);
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("Access denied.");
                return;
            }

            await _next(context);
        }
    }
}
