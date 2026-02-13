using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.DTOs.Product
{
    public record ProductResponse
    (
        int Id,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        string Category,
        string SKU,
        DateTime CreatedAt
    );
}
