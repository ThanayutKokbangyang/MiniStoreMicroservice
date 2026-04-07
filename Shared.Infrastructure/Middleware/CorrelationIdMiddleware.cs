using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeader = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Request.Headers.Append(CorrelationIdHeader, Guid.NewGuid().ToString());
            }

            var correlationId = context.Request.Headers[CorrelationIdHeader].ToString();
            context.Response.Headers.Append(CorrelationIdHeader, correlationId);

            // เก็บลง Items เพื่อใช้ข้าม middleware/service
            context.Items["CorrelationId"] = correlationId;

            await _next(context);
        }
    }
}
