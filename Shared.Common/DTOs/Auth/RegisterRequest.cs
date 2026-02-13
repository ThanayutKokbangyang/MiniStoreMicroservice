using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.DTOs.Auth
{
    public class RegisterRequest
    {
         string Username { get; set; }
         string Email { get; set; }
         string Password { get; set; }
         string ConfirmPassword { get; set; }
    }
}
