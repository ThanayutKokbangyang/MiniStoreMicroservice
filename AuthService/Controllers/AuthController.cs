using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs.Auth;
using Shared.Common.DTOs.Common;
using Shared.Common.Interfaces.Services;
using Shared.Common.Validators.Auth;
using System.Security.Claims;

namespace AuthService.Controllers
{
    // ============================================================
    // Auth Controller - API Version 1
    // Versioning ผ่าน URL Path: /api/v1/auth
    // ============================================================

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// 
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<TokenResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        [ProducesResponseType(typeof(ApiResponse<object>), 422)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validate request
            var validator = new RegisterRequestValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return UnprocessableEntity(ApiResponse<object>.FailResponse(
                    "Validation failed",
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }


        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<TokenResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var validator = new LoginRequestValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return UnprocessableEntity(ApiResponse<object>.FailResponse(
                    "Validation failed",
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            // ส่ง IP Address สำหรับ Security Logging
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.LoginAsync(request, ipAddress);
            return Ok(result);
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<TokenResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Change password (requires authentication)
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.ChangePasswordAsync(userId, request);
            return Ok(result);
        }

        /// <summary>
        /// Logout / Revoke refresh token
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.RevokeTokenAsync(userId);
            return Ok(result);

        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult GetProfile()
        {
            var profile = new
            {
                Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Username = User.FindFirstValue(ClaimTypes.Name),
                Email = User.FindFirstValue(ClaimTypes.Email),
                Role = User.FindFirstValue(ClaimTypes.Role),
            };

            return Ok(ApiResponse<object>.SuccessResponse(profile));
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("/health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "AuthService",
                Version = "1.0",
                Timestamp = DateTime.UtcNow
            });
        }
    }



}
