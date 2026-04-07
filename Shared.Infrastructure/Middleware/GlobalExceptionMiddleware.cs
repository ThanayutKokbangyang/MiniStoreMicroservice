using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Common.DTOs;
using Shared.Common.DTOs.Common;
using Shared.Common.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Shared.Infrastructure.Middleware
{
    // ============================================================
    // Global Exception Handler Middleware
    // Design Pattern: Chain of Responsibility
    // ============================================================
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            int statusCode;
            string message;
            List<string> errors = new();

            switch (exception)
            {
                case ValidationException validationEx:
                    statusCode = 422;
                    message = validationEx.Message;
                    errors = validationEx.ValidationErrors;
                    _logger.LogWarning(exception, "Validation error. TraceId: {TraceId}", traceId);
                    break;

                case NotFoundException:
                    statusCode = 404;
                    message = exception.Message;
                    _logger.LogWarning(exception, "Resource not found. TraceId: {TraceId}", traceId);
                    break;

                case UnauthorizedException:
                    statusCode = 401;
                    message = exception.Message;
                    _logger.LogWarning(exception, "Unauthorized access attempt. TraceId: {TraceId}", traceId);
                    break;

                case ForbiddenException:
                    statusCode = 403;
                    message = exception.Message;
                    _logger.LogWarning(exception, "Forbidden access. TraceId: {TraceId}", traceId);
                    break;

                case ConflictException:
                    statusCode = 409;
                    message = exception.Message;
                    _logger.LogWarning(exception, "Conflict. TraceId: {TraceId}", traceId);
                    break;

                case TooManyRequestException:
                    statusCode = 429;
                    message = exception.Message;
                    _logger.LogWarning(exception, "Rate limit exceeded. TraceId: {TraceId}", traceId);
                    break;

                case AccountLockedException:
                    statusCode = 423;
                    message = exception.Message;
                    _logger.LogWarning(exception, "Account locked. TraceId: {TraceId}", traceId);
                    break;

                default:
                    statusCode = 500;
                    // ไม่ expose internal error message ให้ client - ป้องกัน Information Disclosure
                    message = "An internal server error occurred. Please try again later.";
                    _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Errors = errors,
                TraceId = traceId
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
