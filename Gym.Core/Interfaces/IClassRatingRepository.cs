using Gym.Core.Entities;

namespace Gym.Core.Interfaces;

public interface IClassRatingRepository : IRepository<ClassRating>
{
    /// <summary>The current member's rating for a session, if they have left one (tracked, with member loaded).</summary>
    Task<ClassRating?> GetForMemberAsync(int classSessionId, int memberId, CancellationToken ct = default);

    /// <summary>All ratings for a session, member loaded, newest first (read-only).</summary>
    Task<IReadOnlyList<ClassRating>> ListForSessionAsync(int classSessionId, CancellationToken ct = default);
}
