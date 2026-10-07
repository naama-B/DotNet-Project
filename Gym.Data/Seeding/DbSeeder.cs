using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gym.Data.Seeding;

/// <summary>
/// Applies pending migrations and, on an empty database, inserts enough data to run the
/// system immediately: demo users for every role, instructors, class types with tags, and
/// upcoming sessions (one deliberately near-full to show the booking race).
/// </summary>
public sealed class DbSeeder
{
    private readonly GymDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(GymDbContext db, IPasswordHasher hasher, ILogger<DbSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _db.Database.MigrateAsync(ct);

        if (await _db.Members.AnyAsync(ct))
        {
            _logger.LogInformation("Seed skipped: database already has data.");
            return;
        }

        _logger.LogInformation("Seeding database with demo data.");
        var now = DateTime.UtcNow;

        var admin = new Member
        {
            FullName = "Gym Admin",
            Email = "admin@gym.local",
            PasswordHash = _hasher.Hash("Admin#123"),
            Role = UserRole.Admin,
            CreatedAtUtc = now
        };

        var members = new List<Member>
        {
            new() { FullName = "Dana Cohen", Email = "dana@gym.local", PasswordHash = _hasher.Hash("Member#123"), Role = UserRole.Member, CreatedAtUtc = now },
            new() { FullName = "Noa Levi", Email = "noa@gym.local", PasswordHash = _hasher.Hash("Member#123"), Role = UserRole.Member, CreatedAtUtc = now },
            new() { FullName = "Yael Mizrahi", Email = "yael@gym.local", PasswordHash = _hasher.Hash("Member#123"), Role = UserRole.Member, CreatedAtUtc = now },
            new() { FullName = "Tamar Peretz", Email = "tamar@gym.local", PasswordHash = _hasher.Hash("Member#123"), Role = UserRole.Member, CreatedAtUtc = now }
        };

        var cardio = new Tag { Name = "Cardio" };
        var strength = new Tag { Name = "Strength" };
        var beginner = new Tag { Name = "Beginner" };
        var mind = new Tag { Name = "Mind & Body" };

        var spin = new ClassType { Name = "Spin", Description = "Indoor cycling.", DurationMinutes = 45, DefaultCapacity = 12, Tags = { cardio, beginner } };
        var yoga = new ClassType { Name = "Yoga Flow", Description = "Vinyasa yoga.", DurationMinutes = 60, DefaultCapacity = 15, Tags = { mind, beginner } };
        var hiit = new ClassType { Name = "HIIT", Description = "High intensity intervals.", DurationMinutes = 30, DefaultCapacity = 10, Tags = { cardio, strength } };

        var alex = new Instructor { FullName = "Alex Bar", Bio = "Certified cycling and HIIT coach." };
        var maya = new Instructor { FullName = "Maya Gold", Bio = "Yoga teacher, 500h RYT." };

        // A near-full session: capacity 3, already 2 confirmed -> exactly one spot left.
        var raceSession = new ClassSession
        {
            ClassType = spin,
            Instructor = alex,
            StartsAtUtc = now.AddDays(1).Date.AddHours(18),
            Capacity = 3,
            BookedCount = 2,
            Status = ClassSessionStatus.Scheduled
        };

        // A session that already took place, so its attendees can leave satisfaction ratings.
        var pastSpin = new ClassSession
        {
            ClassType = spin,
            Instructor = alex,
            StartsAtUtc = now.AddDays(-3).Date.AddHours(18),
            Capacity = 12,
            BookedCount = 3,
            Status = ClassSessionStatus.Scheduled
        };

        var sessions = new List<ClassSession>
        {
            raceSession,
            pastSpin,
            new() { ClassType = spin, Instructor = alex, StartsAtUtc = now.AddDays(2).Date.AddHours(7), Capacity = 12, BookedCount = 0, Status = ClassSessionStatus.Scheduled },
            new() { ClassType = yoga, Instructor = maya, StartsAtUtc = now.AddDays(1).Date.AddHours(9), Capacity = 15, BookedCount = 4, Status = ClassSessionStatus.Scheduled },
            new() { ClassType = yoga, Instructor = maya, StartsAtUtc = now.AddDays(3).Date.AddHours(19), Capacity = 15, BookedCount = 0, Status = ClassSessionStatus.Scheduled },
            new() { ClassType = hiit, Instructor = alex, StartsAtUtc = now.AddDays(2).Date.AddHours(17), Capacity = 10, BookedCount = 9, Status = ClassSessionStatus.Scheduled }
        };

        // Two confirmed bookings that back raceSession.BookedCount = 2.
        raceSession.Bookings.Add(new Booking { Member = members[0], Status = BookingStatus.Confirmed, CreatedAtUtc = now });
        raceSession.Bookings.Add(new Booking { Member = members[1], Status = BookingStatus.Confirmed, CreatedAtUtc = now });

        // Three members attended pastSpin; two have already rated it.
        pastSpin.Bookings.Add(new Booking { Member = members[0], Status = BookingStatus.Confirmed, CreatedAtUtc = now.AddDays(-6) });
        pastSpin.Bookings.Add(new Booking { Member = members[1], Status = BookingStatus.Confirmed, CreatedAtUtc = now.AddDays(-6) });
        pastSpin.Bookings.Add(new Booking { Member = members[2], Status = BookingStatus.Confirmed, CreatedAtUtc = now.AddDays(-6) });
        pastSpin.Ratings.Add(new ClassRating { Member = members[0], Stars = 5, Comment = "Great energy, loved the playlist.", CreatedAtUtc = now.AddDays(-2) });
        pastSpin.Ratings.Add(new ClassRating { Member = members[1], Stars = 4, CreatedAtUtc = now.AddDays(-2) });

        await _db.Members.AddAsync(admin, ct);
        await _db.Members.AddRangeAsync(members, ct);
        await _db.Instructors.AddRangeAsync(alex, maya);
        await _db.ClassTypes.AddRangeAsync(spin, yoga, hiit);
        await _db.ClassSessions.AddRangeAsync(sessions, ct);

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seed complete: {Members} members, {Sessions} sessions.", members.Count + 1, sessions.Count);
    }
}
