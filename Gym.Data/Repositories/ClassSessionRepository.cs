using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gym.Data.Repositories;

public sealed class ClassSessionRepository : Repository<ClassSession>, IClassSessionRepository
{
    public ClassSessionRepository(GymDbContext db) : base(db) { }

    public async Task<(IReadOnlyList<ClassSessionListRow> Items, int TotalCount)> QueryAsync(
        ClassSessionQueryParameters p, CancellationToken ct = default)
    {
        IQueryable<ClassSession> query = Set.AsNoTracking();

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
        var sessions = await query
            .Include(s => s.ClassType)
            .Include(s => s.Instructor)
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync(ct);

        // Rating count + average for just this page, aggregated in the database. The list needs
        // only those two numbers per session, so it never loads a single ClassRating row.
        var pageIds = sessions.Select(s => s.Id).ToArray();
        var stats = (await Db.ClassRatings
                .Where(r => pageIds.Contains(r.ClassSessionId))
                .GroupBy(r => r.ClassSessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count(), Average = g.Average(r => (double)r.Stars) })
                .ToListAsync(ct))
            .ToDictionary(x => x.SessionId);

        var items = sessions
            .Select(s => stats.TryGetValue(s.Id, out var st)
                ? new ClassSessionListRow(s, st.Count, Math.Round(st.Average, 2))
                : new ClassSessionListRow(s, 0, null))
            .ToList();

        return (items, total);
    }

    public Task<ClassSession?> GetDetailAsync(int id, CancellationToken ct = default) =>
        Set.AsNoTracking()
            .Include(s => s.ClassType)
            .Include(s => s.Instructor)
            .Include(s => s.Ratings)
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

    public async Task<IReadOnlyList<WaitlistEntry>> GetWaitlistAsync(int classSessionId, CancellationToken ct = default) =>
        await Db.WaitlistEntries
            .AsNoTracking()
            .Include(w => w.Member)
            .Where(w => w.ClassSessionId == classSessionId)
            .OrderBy(w => w.Position)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ClassSession>> ListRatedAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Include(s => s.ClassType)
            .Include(s => s.Instructor)
            .Include(s => s.Ratings).ThenInclude(r => r.Member)
            .Where(s => s.Ratings.Any())
            .OrderByDescending(s => s.StartsAtUtc)
            .ToListAsync(ct);
}
