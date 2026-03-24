using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Exceptions
{
    public class AccountLockedException : AppException
    {
        public AccountLockedException(DateTime lockoutEnd) 
            : base($"Account is locked until {lockoutEnd:yyyy,MM-dd:mm:ss} UTC", 423, "ACCOUNT_LOCKED") { }
    }
}
