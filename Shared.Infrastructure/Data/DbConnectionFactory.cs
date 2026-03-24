using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }

    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlConnectionFactory> _logger;

        public SqlConnectionFactory(IConfiguration configuration, ILogger<SqlConnectionFactory> logger)
        {
            _logger = logger;
            // อ่าน connection string จาก configuration
            // รองรับ environment-specific (UAT/Production)
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            _logger.LogInformation("Database connection factory initialized for environment: {Environment}",
           configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown");
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);
            return connection;
        }
    }

    public class DatabaseInitializer
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(IDbConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                connection.Open();

                _logger.LogInformation("Starting database initialization...");

                var command = (SqlCommand)connection.CreateCommand();
                command.CommandText = DatabaseSchema.CreateTablesScript;
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                throw;
            }
        }
    }
}
public static class DatabaseSchema
{
    public const string CreateTablesScript = @"
        -- =====================================================
        -- Users Table (AuthService)
        -- =====================================================
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
        BEGIN
            CREATE TABLE Users (
                Id              INT IDENTITY(1,1) PRIMARY KEY,
                Username        NVARCHAR(50) NOT NULL UNIQUE,
                Email           NVARCHAR(100) NOT NULL UNIQUE,
                PasswordHash    NVARCHAR(500) NOT NULL,
                PasswordSalt    NVARCHAR(500) NOT NULL,
                Role            NVARCHAR(20) NOT NULL DEFAULT 'User',
                FailedLoginAttempts INT NOT NULL DEFAULT 0,
                LockoutEnd      DATETIME2 NULL,
                RefreshToken    NVARCHAR(500) NULL,
                RefreshTokenExpiryTime DATETIME2 NULL,
                CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt       DATETIME2 NULL,
                CreatedBy       NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                UpdatedBy       NVARCHAR(100) NULL,
                IsActive        BIT NOT NULL DEFAULT 1,
                
                INDEX IX_Users_Username NONCLUSTERED (Username),
                INDEX IX_Users_Email NONCLUSTERED (Email),
                INDEX IX_Users_RefreshToken NONCLUSTERED (RefreshToken)
            );
        END

        -- =====================================================
        -- Products Table (ProductService)
        -- =====================================================
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' AND xtype='U')
        BEGIN
            CREATE TABLE Products (
                Id              INT IDENTITY(1,1) PRIMARY KEY,
                Name            NVARCHAR(200) NOT NULL,
                Description     NVARCHAR(2000) NULL,
                Price           DECIMAL(18,2) NOT NULL,
                StockQuantity   INT NOT NULL DEFAULT 0,
                Category        NVARCHAR(100) NOT NULL,
                SKU             NVARCHAR(50) NOT NULL UNIQUE,
                CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt       DATETIME2 NULL,
                CreatedBy       NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                UpdatedBy       NVARCHAR(100) NULL,
                IsActive        BIT NOT NULL DEFAULT 1,

                INDEX IX_Products_Category NONCLUSTERED (Category),
                INDEX IX_Products_SKU NONCLUSTERED (SKU),
                INDEX IX_Products_Name NONCLUSTERED (Name)
            );
        END

        -- =====================================================
        -- Orders Table (OrderService)
        -- =====================================================
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
        BEGIN
            CREATE TABLE Orders (
                Id              INT IDENTITY(1,1) PRIMARY KEY,
                UserId          INT NOT NULL,
                OrderNumber     NVARCHAR(50) NOT NULL UNIQUE,
                TotalAmount     DECIMAL(18,2) NOT NULL,
                Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending',
                ShippingAddress NVARCHAR(500) NOT NULL,
                CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt       DATETIME2 NULL,
                CreatedBy       NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                UpdatedBy       NVARCHAR(100) NULL,
                IsActive        BIT NOT NULL DEFAULT 1,

                CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
                INDEX IX_Orders_UserId NONCLUSTERED (UserId),
                INDEX IX_Orders_OrderNumber NONCLUSTERED (OrderNumber),
                INDEX IX_Orders_Status NONCLUSTERED (Status)
            );
        END

        -- =====================================================
        -- OrderItems Table
        -- =====================================================
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' AND xtype='U')
        BEGIN
            CREATE TABLE OrderItems (
                Id              INT IDENTITY(1,1) PRIMARY KEY,
                OrderId         INT NOT NULL,
                ProductId       INT NOT NULL,
                Quantity        INT NOT NULL,
                UnitPrice       DECIMAL(18,2) NOT NULL,
                TotalPrice      DECIMAL(18,2) NOT NULL,
                CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt       DATETIME2 NULL,
                CreatedBy       NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                UpdatedBy       NVARCHAR(100) NULL,
                IsActive        BIT NOT NULL DEFAULT 1,

                CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id),
                CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id),
                INDEX IX_OrderItems_OrderId NONCLUSTERED (OrderId)
            );
        END

        -- =====================================================
        -- AuditLogs Table - เก็บ Log ทุก Action
        -- =====================================================
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
        BEGIN
            CREATE TABLE AuditLogs (
                Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
                UserId          INT NULL,
                Action          NVARCHAR(100) NOT NULL,
                EntityType      NVARCHAR(100) NOT NULL,
                EntityId        NVARCHAR(50) NULL,
                OldValues       NVARCHAR(MAX) NULL,
                NewValues       NVARCHAR(MAX) NULL,
                IpAddress       NVARCHAR(45) NULL,
                UserAgent       NVARCHAR(500) NULL,
                ServiceName     NVARCHAR(100) NOT NULL,
                Timestamp       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                TraceId         NVARCHAR(100) NULL,

                INDEX IX_AuditLogs_UserId NONCLUSTERED (UserId),
                INDEX IX_AuditLogs_Timestamp NONCLUSTERED (Timestamp),
                INDEX IX_AuditLogs_EntityType NONCLUSTERED (EntityType),
                INDEX IX_AuditLogs_TraceId NONCLUSTERED (TraceId)
            );
        END

        -- =====================================================
        -- SecurityLogs Table - เก็บ Log ด้านความปลอดภัย
        -- =====================================================
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SecurityLogs' AND xtype='U')
        BEGIN
            CREATE TABLE SecurityLogs (
                Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
                EventType       NVARCHAR(50) NOT NULL,  -- LoginSuccess, LoginFailed, AccountLocked, etc.
                UserId          INT NULL,
                Username        NVARCHAR(50) NULL,
                IpAddress       NVARCHAR(45) NULL,
                UserAgent       NVARCHAR(500) NULL,
                Details         NVARCHAR(MAX) NULL,
                Severity        NVARCHAR(20) NOT NULL DEFAULT 'Info',
                Timestamp       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

                INDEX IX_SecurityLogs_EventType NONCLUSTERED (EventType),
                INDEX IX_SecurityLogs_UserId NONCLUSTERED (UserId),
                INDEX IX_SecurityLogs_Timestamp NONCLUSTERED (Timestamp),
                INDEX IX_SecurityLogs_IpAddress NONCLUSTERED (IpAddress)
            );
        END
    ";
}