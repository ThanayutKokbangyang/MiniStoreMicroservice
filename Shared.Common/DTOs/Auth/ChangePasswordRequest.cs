using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.DTOs.Auth
{
    public class ChangePasswordRequest
    {
        string CurrentPassword { get; set; }
        string NewPassword { get; set; }
        string ConfirmNewPassword { get; set; }
    }
}
