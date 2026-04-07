using Shared.Common.DTOs.Auth;
using Shared.Common.DTOs.Common;
using Shared.Common.Exceptions;
using Shared.Common.Interfaces.Repositories;
using Shared.Common.Interfaces.Services;
using Shared.Common.Models;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Security;

namespace AuthService.Services
{
    public class AuthServiceImpl : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IAuditLogService _auditLog;
        private readonly ILogger<AuthServiceImpl> _logger;
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan RefreshTokenExpiry = TimeSpan.FromDays(7);

        public AuthServiceImpl(IUserRepository userRepository, ITokenService tokenService,IAuditLogService auditLog, ILogger<AuthServiceImpl> logger) 
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _auditLog = auditLog;
            _logger = logger;
        }

        public async Task<ApiResponse<TokenResponse>> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.GetByUsernameAsync(request.Username) != null)
                throw new ConflictException($"Username '{request.Username}' is already taken.");
            if (await _userRepository.GetByEmailAsync(request.Email) != null)
                throw new ConflictException($"Email '{request.Email}' is already registered.");

            var (hash, salt) = PasswordHasher.HashPassword(request.Password);
            var user = new User
            {
                Username = InputSanitizer.SanitizeHtml(request.Username),
                Email = request.Email.ToLower().Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = "User",
                CreatedBy = "SYSTEM"
            };

            user.Id = await _userRepository.CreateAsync(user);
            
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            await _userRepository.UpdateRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.Add(RefreshTokenExpiry));
            await _auditLog.LogAsync(new AuditLogEntry { UserId = user.Id, Action = "Register", EntityType = "User", EntityId = user.Id.ToString(), ServiceName = "AuthService" });
            await _auditLog.LogSecurityEventAsync(new SecurityLogEntry { EventType = "UserRegistered", UserId = user.Id, Username = user.Username, Severity = "Info" });

            return ApiResponse<TokenResponse>.SuccessResponse(new TokenResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15)), "Registration successful");
        }

        Task<ApiResponse<TokenResponse>> IAuthService.LoginAsync(LoginRequest request, string ipAddress)
        {
            throw new NotImplementedException();
        }

        Task<ApiResponse<TokenResponse>> IAuthService.RefreshTokenAsync(RefreshTokenRequest request)
        {
            throw new NotImplementedException();
        }

        Task<ApiResponse<bool>> IAuthService.ChangePasswordAsync(int userId, ChangePasswordRequest changePasswordRequest)
        {
            throw new NotImplementedException();
        }

        Task<ApiResponse<bool>> IAuthService.RevokeTokenAsync(int userId)
        {
            throw new NotImplementedException();
        }
    }
}
