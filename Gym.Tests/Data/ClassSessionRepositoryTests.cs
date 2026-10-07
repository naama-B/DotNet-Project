using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Data.Repositories;
using Gym.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Gym.Tests.Data;

public sealed class ClassSessionRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryContext _db = new();

    public ClassSessionRepositoryTests()
    {
        using var ctx = _db.CreateContext();
        var spin = new ClassType { Name = "Spin", DurationMinutes = 45, DefaultCapacity = 10 };
        var yoga = new ClassType { Name = "Yoga", DurationMinutes = 60, DefaultCapacity = 10 };
        var coach = new Instructor { FullName = "Alex" };

        for (var i = 0; i < 25; i++)
        {
            ctx.ClassSessions.Add(new ClassSession
            {
                ClassType = i % 2 == 0 ? spin : yoga,
                Instructor = coach,
                StartsAtUtc = DateTime.UtcNow.AddDays(1).AddHours(i),
                Capacity = 10,
                BookedCount = i < 3 ? 10 : 0, // first 3 are full
                Status = ClassSessionStatus.Scheduled
            });
        }
        ctx.SaveChanges();
    }

    [Fact]
    public async Task QueryAsync_pages_in_the_database()
    {
        await using var ctx = _db.CreateContext();
        var repo = new ClassSessionRepository(ctx);

        var (page2, total) = await repo.QueryAsync(new ClassSessionQueryParameters { Page = 2, PageSize = 10 });

        Assert.Equal(25, total);
        Assert.Equal(10, page2.Count);
        // page 2 starts after the first 10 by start time
        Assert.True(page2[0].Session.StartsAtUtc < page2[^1].Session.StartsAtUtc);
    }

    [Fact]
    public async Task QueryAsync_filters_by_search_and_availability()
    {
        await using var ctx = _db.CreateContext();
        var repo = new ClassSessionRepository(ctx);

        var (items, total) = await repo.QueryAsync(new ClassSessionQueryParameters
        {
            Search = "spin",
            OnlyAvailable = true,
            PageSize = 100
        });

        Assert.Equal(total, items.Count);
        Assert.All(items, r => Assert.Equal("Spin", r.Session.ClassType.Name));
        Assert.All(items, r => Assert.True(r.Session.BookedCount < r.Session.Capacity));
    }

    public void Dispose() => _db.Dispose();
}
