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
                Id                      INT IDENTITY(1,1) PRIMARY KEY,
                Username                NVARCHAR(50) NOT NULL UNIQUE,
                Email                   NVARCHAR(100) NOT NULL UNIQUE,
                PasswordHash            NVARCHAR(500) NOT NULL,
                PasswordSalt            NVARCHAR(500) NOT NULL,
                Role                    NVARCHAR(20) NOT NULL DEFAULT 'User',
                FailedLoginAttempts     INT NOT NULL DEFAULT 0,
                LockoutEnd              DATETIME NULL,
                RefreshToken            NVARCHAR(500) NULL,
                RefreshTokenExpiryTime  DATETIME NULL,
                CreatedAt               DATETIME NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt               DATETIME NULL,
                CreatedBy               NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                UpdatedBy               NVARCHAR(100) NULL,
                IsActive                BIT NOT NULL DEFAULT 1
            );
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_RefreshToken' AND object_id = OBJECT_ID('Users'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_Users_RefreshToken ON Users(RefreshToken);
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
                CreatedAt       DATETIME NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt       DATETIME NULL,
                CreatedBy       NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                UpdatedBy       NVARCHAR(100) NULL,
                IsActive        BIT NOT NULL DEFAULT 1
            );
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Category' AND object_id = OBJECT_ID('Products'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_Products_Category ON Products(Category);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Name' AND object_id = OBJECT_ID('Products'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_Products_Name ON Products(Name);
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
                CreatedAt       DATETIME NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt       DATETIME NULL,
                CreatedBy       NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                UpdatedBy       NVARCHAR(100) NULL,
                IsActive        BIT NOT NULL DEFAULT 1,
                CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
            );
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_UserId' AND object_id = OBJECT_ID('Orders'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_Orders_UserId ON Orders(UserId);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_Status' AND object_id = OBJECT_ID('Orders'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_Orders_Status ON Orders(Status);
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
                CreatedAt       DATETIME NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt       DATETIME NULL,
                CreatedBy       NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                UpdatedBy       NVARCHAR(100) NULL,
                IsActive        BIT NOT NULL DEFAULT 1,
                CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id),
                CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
            );
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_OrderId' AND object_id = OBJECT_ID('OrderItems'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_ProductId' AND object_id = OBJECT_ID('OrderItems'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_OrderItems_ProductId ON OrderItems(ProductId);
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
                Timestamp       DATETIME NOT NULL DEFAULT GETUTCDATE(),
                TraceId         NVARCHAR(100) NULL
            );
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_UserId' AND object_id = OBJECT_ID('AuditLogs'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_Timestamp' AND object_id = OBJECT_ID('AuditLogs'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_EntityType' AND object_id = OBJECT_ID('AuditLogs'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_AuditLogs_EntityType ON AuditLogs(EntityType);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_TraceId' AND object_id = OBJECT_ID('AuditLogs'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_AuditLogs_TraceId ON AuditLogs(TraceId);
        END

        -- =====================================================
        -- SecurityLogs Table - เก็บ Log ด้านความปลอดภัย
        -- =====================================================
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SecurityLogs' AND xtype='U')
        BEGIN
            CREATE TABLE SecurityLogs (
                Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
                EventType       NVARCHAR(50) NOT NULL,
                UserId          INT NULL,
                Username        NVARCHAR(50) NULL,
                IpAddress       NVARCHAR(45) NULL,
                UserAgent       NVARCHAR(500) NULL,
                Details         NVARCHAR(MAX) NULL,
                Severity        NVARCHAR(20) NOT NULL DEFAULT 'Info',
                Timestamp       DATETIME NOT NULL DEFAULT GETUTCDATE()
            );
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SecurityLogs_EventType' AND object_id = OBJECT_ID('SecurityLogs'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_SecurityLogs_EventType ON SecurityLogs(EventType);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SecurityLogs_UserId' AND object_id = OBJECT_ID('SecurityLogs'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_SecurityLogs_UserId ON SecurityLogs(UserId);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SecurityLogs_Timestamp' AND object_id = OBJECT_ID('SecurityLogs'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_SecurityLogs_Timestamp ON SecurityLogs(Timestamp);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SecurityLogs_IpAddress' AND object_id = OBJECT_ID('SecurityLogs'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_SecurityLogs_IpAddress ON SecurityLogs(IpAddress);
        END
    ";
}