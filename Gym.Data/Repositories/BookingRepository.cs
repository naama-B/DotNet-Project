using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Gym.Data.Repositories;

public sealed class BookingRepository : Repository<Booking>, IBookingRepository
{
    public BookingRepository(GymDbContext db) : base(db) { }

    public Task<Booking?> GetActiveAsync(int classSessionId, int memberId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(
            b => b.ClassSessionId == classSessionId
                 && b.MemberId == memberId
                 && b.Status != BookingStatus.CancelledByMember,
            ct);

    /// <summary>Returns the booking row for this member/session whatever its status (may be a cancelled one).</summary>
    public Task<Booking?> GetAnyAsync(int classSessionId, int memberId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(b => b.ClassSessionId == classSessionId && b.MemberId == memberId, ct);

    public Task<bool> HasActiveBookingAsync(int classSessionId, int memberId, CancellationToken ct = default) =>
        Set.AnyAsync(
            b => b.ClassSessionId == classSessionId
                 && b.MemberId == memberId
                 && b.Status != BookingStatus.CancelledByMember,
            ct);

    public async Task<IReadOnlyList<Booking>> ListForMemberAsync(int memberId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Include(b => b.Member)
            .Include(b => b.ClassSession).ThenInclude(s => s.ClassType)
            .Where(b => b.MemberId == memberId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<Booking?> GetDetailAsync(int bookingId, CancellationToken ct = default) =>
        Set.AsNoTracking()
            .Include(b => b.Member)
            .Include(b => b.ClassSession).ThenInclude(s => s.ClassType)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    public Task<WaitlistEntry?> GetWaitlistEntryAsync(int classSessionId, int memberId, CancellationToken ct = default) =>
        Db.WaitlistEntries.FirstOrDefaultAsync(
            w => w.ClassSessionId == classSessionId && w.MemberId == memberId, ct);

    public async Task<IReadOnlyDictionary<int, int>> GetWaitlistPositionsForMemberAsync(int memberId, CancellationToken ct = default) =>
        await Db.WaitlistEntries
            .AsNoTracking()
            .Where(w => w.MemberId == memberId)
            .ToDictionaryAsync(w => w.ClassSessionId, w => w.Position, ct);

    public void RemoveWaitlistEntry(WaitlistEntry entry) => Db.WaitlistEntries.Remove(entry);

    public async Task AddWaitlistEntryAsync(WaitlistEntry entry, CancellationToken ct = default) =>
        await Db.WaitlistEntries.AddAsync(entry, ct);
}
