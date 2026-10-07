using Gym.Core.Common;
using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Gym.Service.Services;
using Gym.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Gym.Tests.Services;

/// <summary>
/// Rules for leaving a satisfaction rating: the session must have taken place, the member must
/// have held a confirmed booking, and a second rating overwrites the first.
/// </summary>
public sealed class ClassRatingServiceTests
{
    private readonly Mock<IClassRatingRepository> _ratings = new(MockBehavior.Strict);
    private readonly Mock<IClassSessionRepository> _sessions = new(MockBehavior.Strict);
    private readonly Mock<IBookingRepository> _bookings = new(MockBehavior.Strict);

    private ClassRatingService CreateSut() =>
        new(_ratings.Object, _sessions.Object, _bookings.Object, TestMapper.Create(), NullLogger<ClassRatingService>.Instance);

    private static ClassSession Session(DateTime startsAt) => new()
    {
        Id = 1,
        Capacity = 10,
        BookedCount = 3,
        Status = ClassSessionStatus.Scheduled,
        StartsAtUtc = startsAt,
        ClassType = new ClassType { Id = 1, Name = "Spin", DurationMinutes = 45 }
    };

    private static Member Dana => new() { Id = 7, FullName = "Dana" };

    [Fact]
    public async Task RateAsync_records_a_rating_for_an_attended_past_session()
    {
        _sessions.Setup(r => r.GetDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Session(DateTime.UtcNow.AddDays(-1)));
        _bookings.Setup(r => r.GetAnyAsync(1, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Booking { Id = 5, Status = BookingStatus.Confirmed });
        _ratings.SetupSequence(r => r.GetForMemberAsync(1, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClassRating?)null)
            .ReturnsAsync(new ClassRating { Id = 9, ClassSessionId = 1, MemberId = 7, Stars = 5, Member = Dana, CreatedAtUtc = DateTime.UtcNow });
        _ratings.Setup(r => r.AddAsync(It.IsAny<ClassRating>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _ratings.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().RateAsync(7, 1, new CreateClassRatingRequest { Stars = 5, Comment = "Loved it" });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.Stars);
        Assert.Equal("Dana", result.Value.MemberName);
        _ratings.Verify(r => r.AddAsync(It.Is<ClassRating>(x => x.Stars == 5 && x.Comment == "Loved it"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RateAsync_rejects_a_session_that_has_not_happened_yet()
    {
        _sessions.Setup(r => r.GetDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Session(DateTime.UtcNow.AddDays(2)));

        var result = await CreateSut().RateAsync(7, 1, new CreateClassRatingRequest { Stars = 4 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Validation, result.Status);
    }

    [Fact]
    public async Task RateAsync_rejects_a_member_without_a_confirmed_booking()
    {
        _sessions.Setup(r => r.GetDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Session(DateTime.UtcNow.AddDays(-1)));
        _bookings.Setup(r => r.GetAnyAsync(1, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Booking { Id = 5, Status = BookingStatus.Waitlisted });

        var result = await CreateSut().RateAsync(7, 1, new CreateClassRatingRequest { Stars = 4 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task RateAsync_updates_an_existing_rating()
    {
        var existing = new ClassRating { Id = 9, ClassSessionId = 1, MemberId = 7, Stars = 2, Member = Dana, CreatedAtUtc = DateTime.UtcNow.AddDays(-1) };
        _sessions.Setup(r => r.GetDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Session(DateTime.UtcNow.AddDays(-1)));
        _bookings.Setup(r => r.GetAnyAsync(1, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Booking { Id = 5, Status = BookingStatus.Confirmed });
        _ratings.SetupSequence(r => r.GetForMemberAsync(1, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing)
            .ReturnsAsync(existing);
        _ratings.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().RateAsync(7, 1, new CreateClassRatingRequest { Stars = 5 });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, existing.Stars);
        Assert.NotNull(existing.UpdatedAtUtc);
        _ratings.Verify(r => r.AddAsync(It.IsAny<ClassRating>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListForSessionAsync_returns_the_average()
    {
        _sessions.Setup(r => r.GetDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Session(DateTime.UtcNow.AddDays(-1)));
        _ratings.Setup(r => r.ListForSessionAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClassRating>
            {
                new() { Id = 1, Stars = 5, Member = Dana, CreatedAtUtc = DateTime.UtcNow },
                new() { Id = 2, Stars = 4, Member = new Member { Id = 8, FullName = "Noa" }, CreatedAtUtc = DateTime.UtcNow },
            });

        var result = await CreateSut().ListForSessionAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.RatingCount);
        Assert.Equal(4.5, result.Value.AverageStars);
    }

    [Fact]
    public async Task ListForSessionAsync_returns_NotFound_for_an_unknown_session()
    {
        _sessions.Setup(r => r.GetDetailAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((ClassSession?)null);

        var result = await CreateSut().ListForSessionAsync(99);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ListReviewedSessionsAsync_returns_title_instructor_and_ratings_newest_first()
    {
        var session = Session(DateTime.UtcNow.AddDays(-3));
        session.Instructor = new Instructor { Id = 1, FullName = "Alex Bar" };
        session.Ratings = new List<ClassRating>
        {
            new() { Id = 1, Stars = 3, Member = Dana, Comment = "Older", CreatedAtUtc = DateTime.UtcNow.AddDays(-2) },
            new() { Id = 2, Stars = 5, Member = new Member { Id = 8, FullName = "Noa" }, Comment = "Newer", CreatedAtUtc = DateTime.UtcNow.AddHours(-1) },
        };
        _sessions.Setup(r => r.ListRatedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClassSession> { session });

        var result = await CreateSut().ListReviewedSessionsAsync();

        var reviewed = Assert.Single(result);
        Assert.Equal("Spin", reviewed.ClassTypeName);
        Assert.Equal("Alex Bar", reviewed.InstructorName);
        Assert.Equal(2, reviewed.RatingCount);
        Assert.Equal(4, reviewed.AverageStars);
        Assert.Collection(reviewed.Ratings,
            first => Assert.Equal("Newer", first.Comment),
            second => Assert.Equal("Older", second.Comment));
    }
}
