using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Gym.Service.Services;
using Gym.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Gym.Tests.Services;

/// <summary>
/// Service-level tests for the contended operation, with repositories mocked (Moq).
/// Covers the happy path (a spot is taken) and the rejected path (the concurrency token
/// conflict surfaces as a 409-style <c>Conflict</c> result).
/// </summary>
public sealed class BookingServiceTests
{
    private readonly Mock<IClassSessionRepository> _sessions = new(MockBehavior.Strict);
    private readonly Mock<IBookingRepository> _bookings = new(MockBehavior.Strict);

    private BookingService CreateSut() =>
        new(_sessions.Object, _bookings.Object, TestMapper.Create(), NullLogger<BookingService>.Instance);

    private static ClassSession Session(int booked, int capacity = 3) => new()
    {
        Id = 1,
        Capacity = capacity,
        BookedCount = booked,
        Status = ClassSessionStatus.Scheduled,
        StartsAtUtc = DateTime.UtcNow.AddDays(1),
        ClassType = new ClassType { Id = 1, Name = "Spin", DurationMinutes = 45 }
    };

    [Fact]
    public async Task BookAsync_takes_a_spot_when_room_is_available()
    {
        var session = Session(booked: 1);
        _sessions.Setup(r => r.GetForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _bookings.Setup(r => r.GetAnyAsync(1, 7, It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);
        _bookings.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => b.Id = 55)
            .Returns(Task.CompletedTask);
        _bookings.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _bookings.Setup(r => r.GetDetailAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Booking
            {
                Id = 55,
                Status = BookingStatus.Confirmed,
                CreatedAtUtc = DateTime.UtcNow,
                Member = new Member { Id = 7, FullName = "Dana" },
                ClassSession = session
            });

        var result = await CreateSut().BookAsync(7, new CreateBookingRequest { ClassSessionId = 1 });

        Assert.True(result.IsSuccess);
        Assert.Equal("Confirmed", result.Value!.Status);
        Assert.Equal(2, session.BookedCount); // the spot was consumed
        _bookings.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookAsync_returns_Conflict_when_the_concurrency_token_check_fails()
    {
        var session = Session(booked: 2); // last spot
        _sessions.Setup(r => r.GetForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _bookings.Setup(r => r.GetAnyAsync(1, 7, It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);
        _bookings.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _bookings.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("row changed under us"));

        var result = await CreateSut().BookAsync(7, new CreateBookingRequest { ClassSessionId = 1 });

        Assert.False(result.IsSuccess);
        Assert.Equal(Gym.Core.Common.ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task BookAsync_rejects_a_double_booking()
    {
        var session = Session(booked: 1);
        _sessions.Setup(r => r.GetForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _bookings.Setup(r => r.GetAnyAsync(1, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Booking { Id = 9, MemberId = 7, ClassSessionId = 1, Status = BookingStatus.Confirmed });

        var result = await CreateSut().BookAsync(7, new CreateBookingRequest { ClassSessionId = 1 });

        Assert.False(result.IsSuccess);
        Assert.Equal(Gym.Core.Common.ResultStatus.Conflict, result.Status);
        _bookings.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BookAsync_waitlists_when_the_session_is_full()
    {
        var session = Session(booked: 3); // full
        _sessions.Setup(r => r.GetForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _sessions.Setup(r => r.NextWaitlistPositionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _bookings.Setup(r => r.GetAnyAsync(1, 7, It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);
        _bookings.Setup(r => r.AddWaitlistEntryAsync(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _bookings.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => b.Id = 77)
            .Returns(Task.CompletedTask);
        _bookings.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _bookings.Setup(r => r.GetDetailAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Booking
            {
                Id = 77,
                Status = BookingStatus.Waitlisted,
                CreatedAtUtc = DateTime.UtcNow,
                Member = new Member { Id = 7, FullName = "Dana" },
                ClassSession = session
            });

        var result = await CreateSut().BookAsync(7, new CreateBookingRequest { ClassSessionId = 1 });

        Assert.True(result.IsSuccess);
        Assert.Equal("Waitlisted", result.Value!.Status);
        Assert.Equal(1, result.Value.WaitlistPosition);
        Assert.Equal(3, session.BookedCount); // unchanged — no spot consumed
    }

    [Fact]
    public async Task BookAsync_rejects_a_cancelled_session()
    {
        var session = Session(booked: 0);
        session.Status = ClassSessionStatus.Cancelled;
        _sessions.Setup(r => r.GetForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var result = await CreateSut().BookAsync(7, new CreateBookingRequest { ClassSessionId = 1 });

        Assert.False(result.IsSuccess);
        Assert.Equal(Gym.Core.Common.ResultStatus.Validation, result.Status);
    }
}
