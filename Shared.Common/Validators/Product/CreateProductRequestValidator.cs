using FluentValidation;
using Shared.Common.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Validators.Product
{
    public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateProductRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200)
                .Must(NotContainXss).WithMessage("Product name contains invalid characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .Must(NotContainXss).WithMessage("Description contains invalid characters");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0")
                .LessThan(1_000_000).WithMessage("Price must be less than 1,000,000");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative");

            RuleFor(x => x.SKU)
                .NotEmpty().WithMessage("SKU is required")
                .Matches(@"^[A-Z0-9\-]+$").WithMessage("SKU must contain only uppercase letters, numbers, and hyphens");
        }

        private static bool NotContainXss(string? value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            var xssPatterns = new[] { "<script", "javascript:", "onerror", "onload", "eval(" };
            return !xssPatterns.Any(p => value.ToLower().Contains(p));
        }
    }
}
