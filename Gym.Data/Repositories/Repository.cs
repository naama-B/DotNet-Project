using System.Linq.Expressions;
using Gym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gym.Data.Repositories;

/// <summary>
/// Shared EF Core implementation of <see cref="IRepository{T}"/>. All repositories share the
/// same scoped <see cref="GymDbContext"/>, so a call to <see cref="SaveChangesAsync"/> on any
/// of them commits every pending change.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly GymDbContext Db;
    protected readonly DbSet<T> Set;

    public Repository(GymDbContext db)
    {
        Db = db;
        Set = db.Set<T>();
    }

    public virtual Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Set.FindAsync(new object?[] { id }, ct).AsTask();

    public virtual async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Set.AnyAsync(predicate, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await Set.AddAsync(entity, ct);

    public void Remove(T entity) => Set.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Db.SaveChangesAsync(ct);
}
