using Gym.Core.DTOs;
using Gym.Core.Entities;

namespace Gym.Core.Interfaces;

public interface IClassSessionRepository : IRepository<ClassSession>
{
    /// <summary>Read-only paged query executed in the database (Skip/Take, AsNoTracking).</summary>
    Task<(IReadOnlyList<ClassSession> Items, int TotalCount)> QueryAsync(
        ClassSessionQueryParameters parameters, CancellationToken ct = default);

    /// <summary>Loads a session with its class type and instructor for display (read-only).</summary>
    Task<ClassSession?> GetDetailAsync(int id, CancellationToken ct = default);

    /// <summary>Loads a session tracked, for a booking transaction. Includes the class type for duration/name.</summary>
    Task<ClassSession?> GetForUpdateAsync(int id, CancellationToken ct = default);

    Task<int> NextWaitlistPositionAsync(int classSessionId, CancellationToken ct = default);
}
