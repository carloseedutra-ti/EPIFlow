using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Domain.Common;
using Microsoft.EntityFrameworkCore;
using EPIFlow.Infrastructure.Persistence;

namespace EPIFlow.Infrastructure.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly EPIFlowDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public Repository(EPIFlowDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = DbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> AsQueryable() => DbSet.AsQueryable();

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        DbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }
}
