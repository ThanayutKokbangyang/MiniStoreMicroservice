using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.DTOs.Common
{
    public record PaginationRequest
    (
        int PageNumber = 1,
        int PageSize = 10,
        string? SortBy = null,
        string SortDirection = "asc",
        string? SearchTerm = null
    );
}
