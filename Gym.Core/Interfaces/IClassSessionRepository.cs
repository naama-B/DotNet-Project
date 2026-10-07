using Gym.Core.DTOs;
using Gym.Core.Entities;

namespace Gym.Core.Interfaces;

/// <summary>
/// One row of the paged session list: the session (with class type and instructor loaded)
/// plus its rating aggregates, which are computed in the database so no <see cref="ClassRating"/>
/// rows have to be loaded to render the list.
/// </summary>
public sealed record ClassSessionListRow(ClassSession Session, int RatingCount, double? AverageStars);

public interface IClassSessionRepository : IRepository<ClassSession>
{
    /// <summary>Read-only paged query executed in the database (Skip/Take, AsNoTracking).</summary>
    Task<(IReadOnlyList<ClassSessionListRow> Items, int TotalCount)> QueryAsync(
        ClassSessionQueryParameters parameters, CancellationToken ct = default);

    /// <summary>Loads a session with its class type and instructor for display (read-only).</summary>
    Task<ClassSession?> GetDetailAsync(int id, CancellationToken ct = default);

    /// <summary>Loads a session tracked, for a booking transaction. Includes the class type for duration/name.</summary>
    Task<ClassSession?> GetForUpdateAsync(int id, CancellationToken ct = default);

    Task<int> NextWaitlistPositionAsync(int classSessionId, CancellationToken ct = default);

    /// <summary>The session's waiting list, member loaded, ordered by queue position (read-only).</summary>
    Task<IReadOnlyList<WaitlistEntry>> GetWaitlistAsync(int classSessionId, CancellationToken ct = default);

    /// <summary>
    /// Every session that has at least one rating, with its class type, instructor and ratings
    /// (each rating's member loaded), most recently held first (read-only).
    /// </summary>
    Task<IReadOnlyList<ClassSession>> ListRatedAsync(CancellationToken ct = default);
}
