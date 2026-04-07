using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs.Auth;
using Shared.Common.DTOs.Common;
using Shared.Common.Interfaces.Services;
using Shared.Common.Validators.Auth;

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


    }
}
