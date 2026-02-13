using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.DTOs.Auth
{
    public record ChangePasswordRequest
    (
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword
    );
}
