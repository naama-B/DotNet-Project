using Gym.Core.Entities;

namespace Gym.Core.Interfaces;

public interface IBookingRepository : IRepository<Booking>
{
    Task<Booking?> GetActiveAsync(int classSessionId, int memberId, CancellationToken ct = default);

    /// <summary>The booking row for this member/session whatever its status (may be a cancelled one to reactivate).</summary>
    Task<Booking?> GetAnyAsync(int classSessionId, int memberId, CancellationToken ct = default);

    Task<bool> HasActiveBookingAsync(int classSessionId, int memberId, CancellationToken ct = default);

    /// <summary>All bookings for a member, newest first, with session + class type + member loaded.</summary>
    Task<IReadOnlyList<Booking>> ListForMemberAsync(int memberId, CancellationToken ct = default);

    /// <summary>One booking with session, class type and member loaded (read-only), for building a response.</summary>
    Task<Booking?> GetDetailAsync(int bookingId, CancellationToken ct = default);

    Task<WaitlistEntry?> GetWaitlistEntryAsync(int classSessionId, int memberId, CancellationToken ct = default);

    /// <summary>Queue position per session for every waitlist entry this member holds.</summary>
    Task<IReadOnlyDictionary<int, int>> GetWaitlistPositionsForMemberAsync(int memberId, CancellationToken ct = default);

    void RemoveWaitlistEntry(WaitlistEntry entry);

    Task AddWaitlistEntryAsync(WaitlistEntry entry, CancellationToken ct = default);
}
