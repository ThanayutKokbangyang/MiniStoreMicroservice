using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Shared.Infrastructure.Middleware;

// ============================================================
// API Gateway - YARP Reverse Proxy
// Features: Load Balancing, Rate Limiting, SSL Termination,
//           JWT Validation, Request Routing, Health Checks
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ===== Serilog =====
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "ApiGateway")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [Gateway] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .CreateLogger();
builder.Host.UseSerilog();

// ===== YARP Reverse Proxy =====
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ===== JWT Authentication at Gateway Level =====
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// ===== Rate Limiting =====
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.RealIpHeader = "X-Forwarded-For";
    options.HttpStatusCode = 429;
    options.GeneralRules = new List<RateLimitRule>
    {
        new() { Endpoint = "*", Period = "1s", Limit = 10 },
        new() { Endpoint = "*", Period = "1m", Limit = 200 },
        new() { Endpoint = "*", Period = "1h", Limit = 5000 }
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayPolicy", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? new[] { "https://localhost:3000" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// ===== Middleware Pipeline =====
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseIpRateLimiting();
app.UseHttpsRedirection();
app.UseCors("GatewayPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Map YARP reverse proxy
app.MapReverseProxy();

// Health endpoint
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    Service = "API Gateway",
    Status = "Running",
    Version = "1.0",
    Timestamp = DateTime.UtcNow
}));

Log.Information("API Gateway started on {Environment}", app.Environment.EnvironmentName);
app.Run();
