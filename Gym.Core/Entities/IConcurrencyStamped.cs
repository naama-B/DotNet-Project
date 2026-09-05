namespace Gym.Core.Entities;

/// <summary>
/// Marks an entity that carries a self-managed optimistic-concurrency token.
/// The token value is refreshed on every save by <c>GymDbContext.SaveChangesAsync</c>,
/// and EF Core adds its <i>original</i> value to the UPDATE ... WHERE clause.
/// If another transaction changed the row in the meantime, zero rows match and
/// EF Core throws <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>.
/// </summary>
public interface IConcurrencyStamped
{
    Guid Version { get; set; }
}
