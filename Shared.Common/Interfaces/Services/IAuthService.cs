using Shared.Common.DTOs.Auth;
using Shared.Common.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Interfaces.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<TokenResponse>> RegisterAsync(RegisterRequest reqeust);
        Task<ApiResponse<TokenResponse>> LoginAsync(LoginRequest request, string ipAddress);
        Task<ApiResponse<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest changePasswordRequest);
        Task<ApiResponse<bool>> RevokeTokenAsync(int userId);
    }
}
