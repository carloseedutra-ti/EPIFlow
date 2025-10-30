using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Infrastructure.Persistence;

namespace EPIFlow.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly EPIFlowDbContext _dbContext;

    public UnitOfWork(EPIFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
