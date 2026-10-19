using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gym.Data.Repositories;

public sealed class InstructorRepository : Repository<Instructor>, IInstructorRepository
{
    public InstructorRepository(GymDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Instructor>> ListAsync(bool includeInactive, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(i => includeInactive || i.IsActive)
            .OrderBy(i => i.FullName)
            .ToListAsync(ct);

    public Task<bool> HasUpcomingSessionsAsync(int id, DateTime nowUtc, CancellationToken ct = default) =>
        Db.ClassSessions.AnyAsync(s =>
            s.InstructorId == id &&
            s.Status == ClassSessionStatus.Scheduled &&
            s.StartsAtUtc > nowUtc, ct);
}
