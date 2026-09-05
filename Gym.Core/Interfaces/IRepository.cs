using System.Linq.Expressions;

namespace Gym.Core.Interfaces;

/// <summary>
/// Generic persistence gateway. Concrete repositories add query methods that are specific
/// to their aggregate. <see cref="SaveChangesAsync"/> commits every change tracked by the
/// shared unit of work (the scoped DbContext).
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
