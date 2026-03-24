using Shared.Common.DTOs.Common;
using Shared.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Interfaces.Repositories
{
    public interface IGenericRepository<T> where T : BaseEntity {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<PaginatedResponse<T>> GetPaginatedAsync(PaginationRequest request);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
        Task<bool> HardDeleteAsync(int id);
    }
}
