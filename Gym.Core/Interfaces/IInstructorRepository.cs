using Gym.Core.Entities;

namespace Gym.Core.Interfaces;

public interface IInstructorRepository : IRepository<Instructor>
{
    Task<IReadOnlyList<Instructor>> ListAsync(bool includeInactive, CancellationToken ct = default);

    /// <summary>True when the instructor has a scheduled (not cancelled) session that has not started yet.</summary>
    Task<bool> HasUpcomingSessionsAsync(int id, DateTime nowUtc, CancellationToken ct = default);
}
