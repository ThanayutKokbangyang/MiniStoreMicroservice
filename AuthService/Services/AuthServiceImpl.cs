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

        public async Task<ApiResponse<TokenResponse>> LoginAsync(LoginRequest request, string ipAddress)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                await _auditLog.LogSecurityEventAsync(new SecurityLogEntry { EventType = "LoginFailed", Username = request.Username, Details = "User not found", Severity = "Warning" });
                throw new UnauthorizedException("Invalid username or password.");
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                throw new AccountLockedException(user.LockoutEnd.Value);

            if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                await _userRepository.IncrementFailedLoginAsync(user.Id);
                if (user.FailedLoginAttempts + 1 >= MaxFailedAttempts)
                {
                    var lockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                    await _userRepository.LockUserAsync(user.Id, lockoutEnd);
                    await _auditLog.LogSecurityEventAsync(new SecurityLogEntry { EventType = "AccountLocked", UserId = user.Id, Username = user.Username, Severity = "Critical" });
                    throw new AccountLockedException(lockoutEnd);
                }
                throw new UnauthorizedException("Invalid username or password.");
            }

            await _userRepository.ResetFailedLoginAsync(user.Id);
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            await _userRepository.UpdateRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.Add(RefreshTokenExpiry));

            await _auditLog.LogSecurityEventAsync(new SecurityLogEntry { EventType = "LoginSuccess", UserId = user.Id, Username = user.Username, Severity = "Info" });

            return ApiResponse<TokenResponse>.SuccessResponse(new TokenResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15)));

        }

        public async Task<ApiResponse<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken);
            if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");


            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            await _userRepository.UpdateRefreshTokenAsync(user.Id, newRefreshToken, DateTime.UtcNow.Add(RefreshTokenExpiry));
            return ApiResponse<TokenResponse>.SuccessResponse(new TokenResponse(newAccessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(15)));
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId) ?? throw new NotFoundException("User", userId);
            if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Current password is incorrect.");

            var (hash, salt) = PasswordHasher.HashPassword(request.NewPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            await _userRepository.UpdateAsync(user);

            // Revoke all token on password change
            await _userRepository.UpdateRefreshTokenAsync(userId, string.Empty, DateTime.UtcNow);
            await _auditLog.LogSecurityEventAsync(new SecurityLogEntry { EventType = "PasswordChange", UserId = userId, Severity = "Info" });
            
            return ApiResponse<bool>.SuccessResponse(true, "Password change successfully");
        }

        public async Task<ApiResponse<bool>> RevokeTokenAsync(int userId)
        {
            await _userRepository.UpdateRefreshTokenAsync(userId, string.Empty, DateTime.UtcNow);
            await _auditLog.LogSecurityEventAsync(new SecurityLogEntry { EventType = "TokenRevoked", UserId = userId, Severity = "Info" });
            return ApiResponse<bool>.SuccessResponse(true, "Token revoked successfully");
        }
    }
}
