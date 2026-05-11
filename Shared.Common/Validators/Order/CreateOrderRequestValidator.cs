using FluentValidation;
using Shared.Common.DTOs.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Validators.Order
{
    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.ShippingAddress)
                .NotEmpty().WithMessage("Shipping address is required")
                .MaximumLength(500);

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Order must have at least one item");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId).GreaterThan(0);
                item.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100);
            });
        }
    }
}
