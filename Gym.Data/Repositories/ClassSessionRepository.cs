using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gym.Data.Repositories;

public sealed class ClassSessionRepository : Repository<ClassSession>, IClassSessionRepository
{
    public ClassSessionRepository(GymDbContext db) : base(db) { }

    public async Task<(IReadOnlyList<ClassSession> Items, int TotalCount)> QueryAsync(
        ClassSessionQueryParameters p, CancellationToken ct = default)
    {
        // Read-only query: no tracking, and Include so the projection does not trigger N+1.
        IQueryable<ClassSession> query = Set
            .AsNoTracking()
            .Include(s => s.ClassType)
            .Include(s => s.Instructor);

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var term = p.Search.Trim().ToLower();
            query = query.Where(s => s.ClassType.Name.ToLower().Contains(term));
        }

        if (p.FromUtc is { } from)
            query = query.Where(s => s.StartsAtUtc >= from);

        if (p.OnlyAvailable == true)
            query = query.Where(s => s.Status == ClassSessionStatus.Scheduled && s.BookedCount < s.Capacity);

        query = (p.SortBy?.ToLowerInvariant(), p.SortDescending) switch
        {
            ("available", false) => query.OrderByDescending(s => s.Capacity - s.BookedCount).ThenBy(s => s.StartsAtUtc),
            ("available", true) => query.OrderBy(s => s.Capacity - s.BookedCount).ThenBy(s => s.StartsAtUtc),
            (_, true) => query.OrderByDescending(s => s.StartsAtUtc),
            _ => query.OrderBy(s => s.StartsAtUtc)
        };

        var total = await query.CountAsync(ct);

        // Real pagination: Skip/Take run in the database, not in memory.
        var items = await query
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<ClassSession?> GetDetailAsync(int id, CancellationToken ct = default) =>
        Set.AsNoTracking()
            .Include(s => s.ClassType)
            .Include(s => s.Instructor)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<ClassSession?> GetForUpdateAsync(int id, CancellationToken ct = default) =>
        Set.Include(s => s.ClassType)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<int> NextWaitlistPositionAsync(int classSessionId, CancellationToken ct = default)
    {
        var last = await Db.WaitlistEntries
            .Where(w => w.ClassSessionId == classSessionId)
            .MaxAsync(w => (int?)w.Position, ct);
        return (last ?? 0) + 1;
    }
}
