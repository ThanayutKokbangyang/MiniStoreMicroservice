using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Logging
{
    // ============================================================
    // Audit Log Service - เก็บ Log ทุก Action ลง Database
    // Design Pattern: Observer Pattern
    // ============================================================
    public class AuditLogService : IAuditLogService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(IDbConnectionFactory connectionFactory,IHttpContextAccessor httpContextAccessor,ILogger<AuditLogService> logger)
        {
            _connectionFactory = connectionFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(AuditLogEntry entry)
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                using var connection = _connectionFactory.CreateConnection();

                var sql = @"INSERT INTO AuditLogs 
                (UserId, Action, EntityType, EntityId, OldValues, NewValues, 
                 IpAddress, UserAgent, ServiceName, TraceId)
                 VALUES 
                 (@UserId, @Action, @EntityType, @EntityId, @OldValues, @NewValues, 
                 @IpAddress, @UserAgent, @ServiceName, @TraceId)";

                await connection.ExecuteAsync(sql, new
                {
                    entry.UserId,
                    entry.Action,
                    entry.EntityType,
                    entry.EntityId,
                    OldValues = entry.OldValues != null ? JsonSerializer.Serialize(entry.OldValues) : null,
                    NewValues = entry.NewValues != null ? JsonSerializer.Serialize(entry.NewValues) : null,
                    IpAddress = GetClientIp(context),
                    UserAgent = context?.Request.Headers.UserAgent.ToString(),
                    entry.ServiceName,
                    TraceId = context?.TraceIdentifier
                });
            }
            catch (Exception ex)
            {
                // Log failure ไม่ควรทำให้ main flow fail
                _logger.LogError(ex, "Failed to write audit log for action: {Action}", entry.Action);
            }
        }

        public async Task LogSecurityEventAsync(SecurityLogEntry entry)
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                using var connection = _connectionFactory.CreateConnection();

                var sql = @"INSERT INTO SecurityLogs 
                (EventType, UserId, Username, IpAddress, UserAgent, Details, Severity)
                VALUES 
                (@EventType, @UserId, @Username, @IpAddress, @UserAgent, @Details, @Severity)";

                await connection.ExecuteAsync(sql, new
                {
                    entry.EventType,
                    entry.UserId,
                    entry.Username,
                    IpAddress = GetClientIp(context),
                    UserAgent = context?.Request.Headers.UserAgent.ToString(),
                    entry.Details,
                    entry.Severity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write security log for event: {EventType}", entry.EventType);
            }
        }

        private static string? GetClientIp(HttpContext? context)
        {
            if (context == null) return null;
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return !string.IsNullOrEmpty(forwardedFor)
                ? forwardedFor.Split(',').First().Trim()
                : context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
