using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            // Log Request
            _logger.LogInformation(
                "HTTP {Method} {Path} started. TraceId: {TraceId}, IP: {IP}, UserAgent: {UserAgent}",
                context.Request.Method, context.Request.Path, traceId, GetClientIpAddress(context), context.Request.Headers.UserAgent.ToString());

            await _next(context);

            stopwatch.Stop();

            // Log Response
            _logger.LogInformation(
                "HTTP {Method} {Path} completed with {StatusCode} in {Elapsed}ms. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                traceId);
        }
        private static string GetClientIpAddress(HttpContext context)
        {
            // รองรับ reverse proxy / load balancer
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

    }
}
