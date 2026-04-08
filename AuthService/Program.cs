using Asp.Versioning;
using AspNetCoreRateLimit;
using AuthService.Services;
using Serilog;
using Serilog.Events;
using Shared.Common.Interfaces;
using Shared.Common.Interfaces.Services;
using Shared.Infrastructure.Data;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Middleware;

// ============================================================
// AuthService - Program.cs
// Microservice สำหรับ Authentication & Authorization
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ===== 1. Serilog Configuration =====
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("ServiceName", "AuthService")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/authservice-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ServiceName}] [{TraceId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://localhost:5341") // Centralized logging
    .CreateLogger();

builder.Host.UseSerilog();

// ===== 2. Services Registration =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Product Service API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Rate Limiting - ป้องกัน DDoS / Brute Force
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.RealIpHeader = "X-Forwarded-For";
    options.ClientIdHeader = "X-ClientId";
    options.HttpStatusCode = 429;
    options.GeneralRules = new List<RateLimitRule>
    {
        // Login endpoint - จำกัด 5 requests per minute per IP
        new() { Endpoint = "POST:/api/v1/auth/login", Period = "1m", Limit = 5 },
        // Register endpoint - จำกัด 3 requests per minute per IP
        new() { Endpoint = "POST:/api/v1/auth/register", Period = "1m", Limit = 3 },
        // General API - 100 requests per minute
        new() { Endpoint = "*", Period = "1m", Limit = 100 }
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// Shared Infrastructure (DB, JWT, Repositories, Cache, Audit)
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Auth Business Logic Service
builder.Services.AddScoped<IAuthService, AuthServiceImpl>();

// CORS - กำหนด Origin ที่อนุญาต
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "https://localhost:3000" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// ===== 3. Middleware Pipeline =====
// Order สำคัญมาก!

// 1. Security Headers (ต้องมาก่อน)
app.UseMiddleware<SecurityHeadersMiddleware>();

// 2. Correlation ID
app.UseMiddleware<CorrelationIdMiddleware>();

// 3. Global Exception Handler
app.UseMiddleware<GlobalExceptionMiddleware>();

// 4. Request Logging
app.UseMiddleware<RequestLoggingMiddleware>();

// 5. IP Filter
app.UseMiddleware<IpFilterMiddleware>();

// 6. Rate Limiting
app.UseIpRateLimiting();

// 7. HTTPS Redirection
app.UseHttpsRedirection();

// 8. CORS
app.UseCors("AllowSpecificOrigins");

// 9. Swagger (Development/UAT only)
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "UAT")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service V1");
        c.RoutePrefix = "swagger";
    });
}

// 10. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 11. Map Controllers
app.MapControllers();
app.MapHealthChecks("/health");

// ===== 4. Database Initialization =====
using (var scope = app.Services.CreateScope())
{
    var dbInit = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await dbInit.InitializeAsync();
}

Log.Information("AuthService started on {Environment}", app.Environment.EnvironmentName);
app.Run();
