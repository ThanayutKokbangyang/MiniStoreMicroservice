# Microservice Portfolio

> A production-style **.NET 8 Microservices Portfolio** featuring API Gateway routing, JWT authentication, structured logging, security hardening, Docker-based deployment, and scalable service architecture.

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Architecture](#-architecture)
- [Tech Stack](#-tech-stack)
- [Design Patterns](#-design-patterns)
- [Security Features](#-security-features)
- [Project Structure](#-project-structure)
- [Getting Started](#-getting-started)
- [API Documentation](#-api-documentation)
- [Scaling](#-scaling)

---

## ✨ Overview

This project demonstrates a practical microservices architecture built with **.NET 8**. It includes an API Gateway, multiple backend services, centralized logging, authentication, authorization, rate limiting, and containerized deployment.

### Key Highlights

- 🔐 JWT authentication with refresh token rotation
- 🚪 API Gateway using YARP
- ⚖️ Load balancing with Nginx and YARP
- 🧱 Clean separation between services and shared infrastructure
- 📦 Docker and Docker Compose support
- 📊 Centralized structured logging with Serilog and Seq
- 🛡️ Security-focused API design
  
---

## 🏛️ Architecture

```text
                        ┌─────────────┐
                        │   Client    │
                        │  Browser    │
                        └──────┬──────┘
                               │ HTTPS / TLS 1.2+
                        ┌──────▼──────┐
                        │    Nginx    │
                        │ SSL + Rate  │
                        │  Limiting   │
                        └──────┬──────┘
                               │
                        ┌──────▼──────┐
                        │ API Gateway │
                        │    YARP     │
                        │ JWT Verify  │
                        └──────┬──────┘
                    ┌──────────┼──────────┐
                    │          │          │
             ┌──────▼──┐ ┌────▼─────┐ ┌──▼───────┐
             │  Auth   │ │ Product  │ │  Order   │
             │ Service │ │ Service  │ │ Service  │
             │  x2 LB  │ │  x2 LB   │ │  x2 LB   │
             └────┬────┘ └────┬─────┘ └────┬─────┘
                  │           │            │
                  └───────────┼────────────┘
                       ┌──────▼──────┐
                       │ SQL Server  │
                       │ Shared DB   │
                       └──────┬──────┘
                              │
                       ┌──────▼──────┐
                       │     Seq     │
                       │ Log Server  │
                       └─────────────┘
```

---

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
| --- | --- | --- |
| Runtime | .NET 8 | Main application framework |
| ORM | Dapper | Lightweight data access with high performance |
| Database | SQL Server 2022 | Relational database |
| API Gateway | YARP | Reverse proxy, routing, and load balancing |
| Authentication | JWT Bearer Token | Stateless authentication |
| Logging | Serilog + Seq | Structured and centralized logging |
| Validation | FluentValidation | Request and input validation |
| Containerization | Docker + Docker Compose | Local and production-like deployment |
| Load Balancing | Nginx + YARP | SSL termination and service distribution |
| Rate Limiting | AspNetCoreRateLimit | Basic API abuse and DDoS protection |

---

## 🎨 Design Patterns

| Pattern | Usage |
| --- | --- |
| Repository Pattern | Separates data access logic with `IGenericRepository<T>` |
| Service Layer Pattern | Encapsulates business logic through services such as `IAuthService` and `IProductService` |
| Factory Pattern | Creates database connections using `IDbConnectionFactory` |
| Strategy Pattern | Allows cache implementation switching through `ICacheService` |
| Template Method | Provides reusable behavior in the generic repository base class |
| Chain of Responsibility | Handles request flow through the middleware pipeline |
| Observer Pattern | Supports audit logging behavior |
| Facade Pattern | Simplifies complex operations behind service interfaces |
| Builder Pattern | Organizes dependency injection registration with extension methods |
| API Gateway Pattern | Centralizes routing through YARP |

---

## 🔒 Security Features

### Authentication & Authorization

- ✅ JWT Bearer Token with access and refresh tokens
- ✅ Refresh token rotation
- ✅ BCrypt password hashing with work factor 12
- ✅ Role-based authorization for Admin and User roles
- ✅ Account lockout after 5 failed login attempts for 15 minutes

### API Security

- ✅ Per-IP and per-endpoint rate limiting
- ✅ CORS whitelist configuration
- ✅ Input validation using FluentValidation
- ✅ API versioning via URL and header

### Network Security

- ✅ HTTPS / TLS 1.2+ through Nginx SSL termination
- ✅ HSTS support
- ✅ Security headers for XSS, clickjacking, and MIME sniffing protection
- ✅ IP filtering and blocking support

### Data Security

- ✅ Parameterized queries with Dapper to prevent SQL injection
- ✅ Input sanitization to reduce XSS risk
- ✅ Soft delete for data recovery
- ✅ No sensitive data exposed in error responses
- ✅ No server version exposure

### Monitoring & Audit

- ✅ Audit logs for CRUD operations
- ✅ Security logs for login attempts and account lockouts
- ✅ Request and response logging
- ✅ Correlation ID for cross-service tracing
- ✅ Centralized logs with Seq

---

## 📁 Project Structure

```text
MicroservicePortfolio/
├── src/
│   ├── ApiGateway/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── Services/
│   │   ├── AuthService/
│   │   │   ├── Controllers/
│   │   │   ├── Services/
│   │   │   ├── Program.cs
│   │   │   ├── appsettings.json
│   │   │   ├── appsettings.UAT.json
│   │   │   └── appsettings.Production.json
│   │   │
│   │   ├── ProductService/
│   │   └── OrderService/
│   │
│   └── Shared/
│       ├── Common/
│       └── Infrastructure/
│
├── tests/
├── Dockerfile
├── docker-compose.yml
└── nginx.conf
```

---

## 🚀 Getting Started

### Prerequisites

Make sure the following tools are installed before running the project:

- .NET 8 SDK
- Docker Desktop
- SQL Server or SQL Server Docker image
- `sqlcmd` command-line tool

### Local Development

#### 1. Start SQL Server

```bash
docker run \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

#### 2. Run Database Migrations

```bash
sqlcmd \
  -S localhost \
  -U sa \
  -P "YourStrong@Passw0rd" \
  -i database/migrations/V001_InitialSchema.sql
```

#### 3. Run Services

Open separate terminals for each service:

```bash
cd src/Services/AuthService
dotnet run
```

```bash
cd src/Services/ProductService
dotnet run
```

```bash
cd src/ApiGateway
dotnet run
```

---

## 📡 API Documentation

### Auth Service

Base route: `/api/v1/auth`

| Method | Endpoint | Auth Required | Description |
| --- | --- | --- | --- |
| POST | `/register` | No | Register a new user |
| POST | `/login` | No | Login and receive tokens |
| POST | `/refresh-token` | No | Refresh access token |
| POST | `/change-password` | Yes | Change user password |
| POST | `/logout` | Yes | Revoke refresh token |
| GET | `/profile` | Yes | Get current user profile |

### Product Service

Base route: `/api/v1/products`

| Method | Endpoint | Auth Required | Description |
| --- | --- | --- | --- |
| GET | `/` | Yes | List products with pagination |
| GET | `/{id}` | Yes | Get product by ID |
| POST | `/` | Admin | Create product |
| PUT | `/{id}` | Admin | Update product |
| DELETE | `/{id}` | Admin | Soft delete product |
| GET | `/search?term=` | Yes | Search products |

### Order Service

Base route: `/api/v1/orders`

| Method | Endpoint | Auth Required | Description |
| --- | --- | --- | --- |
| GET | `/` | Yes | List current user's orders |
| GET | `/{id}` | Yes | Get order detail |
| POST | `/` | Yes | Create order |
| PUT | `/{id}/status` | Admin | Update order status |
| DELETE | `/{id}` | Yes | Cancel order |

---

## 📊 Scaling

Scale a service using Docker Compose:

```bash
docker-compose up -d --scale auth-service=3
```

YARP will automatically load balance traffic across available service instances.

---

## ✅ Notes

This README is designed for portfolio presentation, technical review, and GitHub project documentation. Update environment variables, database credentials, and production secrets before deploying to a real environment.
