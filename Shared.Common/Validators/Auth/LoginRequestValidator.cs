using FluentValidation;
using Shared.Common.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Validators.Auth
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MaximumLength(50);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MaximumLength(128);
        }
    }
}
