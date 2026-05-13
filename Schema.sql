USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'MiniStoreDB')
BEGIN
    CREATE DATABASE MiniStoreDB;
END
GO

USE MiniStoreDB;
GO

-- Users Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
CREATE TABLE Users (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    Username            NVARCHAR(50) NOT NULL,
    Email               NVARCHAR(100) NOT NULL,
    PasswordHash        NVARCHAR(500) NOT NULL,
    PasswordSalt        NVARCHAR(500) NOT NULL,
    Role                NVARCHAR(20) NOT NULL DEFAULT 'User',
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    LockoutEnd          DATETIME2 NULL,
    RefreshToken        NVARCHAR(500) NULL,
    RefreshTokenExpiryTime DATETIME2 NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NULL,
    CreatedBy           NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
    UpdatedBy           NVARCHAR(100) NULL,
    IsActive            BIT NOT NULL DEFAULT 1,

    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

CREATE NONCLUSTERED INDEX IX_Users_Username ON Users(Username);
CREATE NONCLUSTERED INDEX IX_Users_Email ON Users(Email);
CREATE NONCLUSTERED INDEX IX_Users_RefreshToken ON Users(RefreshToken);
GO

-- Products Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' AND xtype='U')
CREATE TABLE Products (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(2000) NULL,
    Price           DECIMAL(18,2) NOT NULL,
    StockQuantity   INT NOT NULL DEFAULT 0,
    Category        NVARCHAR(100) NOT NULL,
    SKU             NVARCHAR(50) NOT NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NULL,
    CreatedBy       NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
    UpdatedBy       NVARCHAR(100) NULL,
    IsActive        BIT NOT NULL DEFAULT 1,

    CONSTRAINT UQ_Products_SKU UNIQUE (SKU)
);
GO

CREATE NONCLUSTERED INDEX IX_Products_Category ON Products(Category);
CREATE NONCLUSTERED INDEX IX_Products_SKU ON Products(SKU);
GO

-- Orders Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
CREATE TABLE Orders (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT NOT NULL,
    OrderNumber     NVARCHAR(50) NOT NULL,
    TotalAmount     DECIMAL(18,2) NOT NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    ShippingAddress NVARCHAR(500) NOT NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NULL,
    CreatedBy       NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
    UpdatedBy       NVARCHAR(100) NULL,
    IsActive        BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT UQ_Orders_OrderNumber UNIQUE (OrderNumber)
);
GO

-- OrderItems Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' AND xtype='U')
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
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
GO

-- AuditLogs Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
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
    TraceId         NVARCHAR(100) NULL
);
GO

-- SecurityLogs Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SecurityLogs' AND xtype='U')
CREATE TABLE SecurityLogs (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    EventType       NVARCHAR(50) NOT NULL,
    UserId          INT NULL,
    Username        NVARCHAR(50) NULL,
    IpAddress       NVARCHAR(45) NULL,
    UserAgent       NVARCHAR(500) NULL,
    Details         NVARCHAR(MAX) NULL,
    Severity        NVARCHAR(20) NOT NULL DEFAULT 'Info',
    Timestamp       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Seed Admin User (password: Admin@123456)
INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, Role, CreatedBy)
SELECT 'admin', 'admin@ministore.com',
       '$2a$12$LJ3m4ys3Sz.iYzVd2m3Ky.dummy.hash.replace.with.actual',
       '$2a$12$LJ3m4ys3Sz.iYzVd2m3Ky.',
       'Admin', 'SYSTEM'
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin');
GO

PRINT 'Migration V001 completed successfully.';
GO