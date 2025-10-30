using System.Threading;
using System.Threading.Tasks;

namespace EPIFlow.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
