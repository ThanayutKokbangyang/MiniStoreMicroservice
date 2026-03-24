using Shared.Common.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Interfaces.Infrastructure
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IProductRepository Products { get; }
        IOrderRepository Orders { get; }
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
