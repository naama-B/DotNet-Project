using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Gym.Tests.Concurrency;

/// <summary>
/// The proof required by part ג of the brief: two independent units of work read the same
/// <see cref="ClassSession"/>, both try to take the last spot, the first save wins and the
/// second fails with <see cref="DbUpdateConcurrencyException"/>. Runs against a real
/// (in-memory SQLite) database so the UPDATE ... WHERE Version = @original round-trip is real.
/// </summary>
public sealed class OptimisticConcurrencyTests : IDisposable
{
    private readonly SqliteInMemoryContext _db = new();

    [Fact]
    public async Task Two_bookings_for_the_last_spot_only_one_save_succeeds()
    {
        // Arrange: a session with exactly one free spot, and two members racing for it.
        int sessionId, memberA, memberB;
        await using (var seed = _db.CreateContext())
        {
            var m1 = new Member { FullName = "Racer A", Email = "a@gym.local", PasswordHash = "x" };
            var m2 = new Member { FullName = "Racer B", Email = "b@gym.local", PasswordHash = "x" };
            var session = new ClassSession
            {
                ClassType = new ClassType { Name = "Spin", DurationMinutes = 45, DefaultCapacity = 3 },
                Instructor = new Instructor { FullName = "Alex" },
                StartsAtUtc = DateTime.UtcNow.AddDays(1),
                Capacity = 3,
                BookedCount = 2,
                Status = ClassSessionStatus.Scheduled
            };
            seed.AddRange(m1, m2, session);
            await seed.SaveChangesAsync();
            sessionId = session.Id;
            memberA = m1.Id;
            memberB = m2.Id;
        }

        await using var ctxA = _db.CreateContext();
        await using var ctxB = _db.CreateContext();

        var sessionA = await ctxA.ClassSessions.SingleAsync(s => s.Id == sessionId);
        var sessionB = await ctxB.ClassSessions.SingleAsync(s => s.Id == sessionId);

        // Both readers saw the same state: 2 of 3 booked.
        Assert.Equal(2, sessionA.BookedCount);
        Assert.Equal(2, sessionB.BookedCount);

        // Act: both take the last spot.
        sessionA.BookedCount++;
        sessionA.Bookings.Add(new Booking { MemberId = memberA, Status = BookingStatus.Confirmed, CreatedAtUtc = DateTime.UtcNow });

        sessionB.BookedCount++;
        sessionB.Bookings.Add(new Booking { MemberId = memberB, Status = BookingStatus.Confirmed, CreatedAtUtc = DateTime.UtcNow });

        // First save wins.
        await ctxA.SaveChangesAsync();

        // Second save loses: it targets a row whose Version no longer matches.
        var conflict = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctxB.SaveChangesAsync());
        Assert.NotNull(conflict);

        // Assert: the database holds exactly one extra booking, capacity is respected.
        await using var verify = _db.CreateContext();
        var final = await verify.ClassSessions.Include(s => s.Bookings).SingleAsync(s => s.Id == sessionId);
        Assert.Equal(3, final.BookedCount);
        Assert.Equal(1, final.Bookings.Count(b => b.Status == BookingStatus.Confirmed));
        Assert.True(final.BookedCount <= final.Capacity);
    }

    [Fact]
    public async Task A_save_based_on_fresh_data_succeeds_after_reload()
    {
        int sessionId;
        await using (var seed = _db.CreateContext())
        {
            var session = new ClassSession
            {
                ClassType = new ClassType { Name = "Yoga", DurationMinutes = 60, DefaultCapacity = 5 },
                Instructor = new Instructor { FullName = "Maya" },
                StartsAtUtc = DateTime.UtcNow.AddDays(1),
                Capacity = 5,
                BookedCount = 0,
                Status = ClassSessionStatus.Scheduled
            };
            seed.ClassSessions.Add(session);
            await seed.SaveChangesAsync();
            sessionId = session.Id;
        }

        await using var ctxA = _db.CreateContext();
        await using var ctxB = _db.CreateContext();

        var a = await ctxA.ClassSessions.SingleAsync(s => s.Id == sessionId);
        var b = await ctxB.ClassSessions.SingleAsync(s => s.Id == sessionId);

        a.BookedCount++;
        await ctxA.SaveChangesAsync();

        b.BookedCount++;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctxB.SaveChangesAsync());

        // The losing caller reloads and retries — now it works.
        await ctxB.Entry(b).ReloadAsync();
        b.BookedCount++;
        await ctxB.SaveChangesAsync();

        await using var verify = _db.CreateContext();
        var final = await verify.ClassSessions.SingleAsync(s => s.Id == sessionId);
        Assert.Equal(2, final.BookedCount);
    }

    public void Dispose() => _db.Dispose();
}
