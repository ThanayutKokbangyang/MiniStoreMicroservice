using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.DTOs.Order
{
    public record OrderResponse
    (
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice
    );
}
