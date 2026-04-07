using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Middleware
{
    // ============================================================
    // Security Headers Middleware
    // ป้องกัน: XSS, Clickjacking, MIME Sniffing, etc.
    // ============================================================
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // ===== Security Headers =====
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            // ป้องกัน Clickjacking
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            // Content Security Policy
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:;");

            // HSTS - บังคับใช้ HTTPS
            context.Response.Headers.Append("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");

            // ป้องกัน Information Disclosure
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("Permissions-Policy",
                "camera=(), microphone=(), geolocation=(), payment=()");

            // ซ่อน Server Header
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            // Cache Control สำหรับ API
            context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
            context.Response.Headers.Append("Pragma", "no-cache");

            await _next(context);
        }
    }
}
