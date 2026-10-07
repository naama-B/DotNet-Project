using Gym.Core.Entities;
using Gym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gym.Data.Repositories;

public sealed class ClassRatingRepository : Repository<ClassRating>, IClassRatingRepository
{
    public ClassRatingRepository(GymDbContext db) : base(db) { }

    public Task<ClassRating?> GetForMemberAsync(int classSessionId, int memberId, CancellationToken ct = default) =>
        Set.Include(r => r.Member)
            .FirstOrDefaultAsync(r => r.ClassSessionId == classSessionId && r.MemberId == memberId, ct);

    public async Task<IReadOnlyList<ClassRating>> ListForSessionAsync(int classSessionId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Include(r => r.Member)
            .Where(r => r.ClassSessionId == classSessionId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);
}
