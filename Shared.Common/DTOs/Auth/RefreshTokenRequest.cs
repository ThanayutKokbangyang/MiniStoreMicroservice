using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.DTOs.Auth
{
    public class RefreshTokenRequest
    {
         string AccessToken { get; set; }
         string RefreshToken { get; set; }
    }
}
