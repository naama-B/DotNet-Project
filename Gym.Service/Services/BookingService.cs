using AutoMapper;
using Gym.Core.Common;
using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gym.Service.Services;

/// <summary>
/// Owns the contended operation. A <see cref="ClassSession"/> has a fixed capacity; when the
/// last spot is booked by two members at once, exactly one save wins and the other is rejected
/// with <see cref="ResultStatus.Conflict"/> (HTTP 409).
///
/// How the guarantee holds:
///  * The business check (is there room?) and the write (BookedCount + 1, plus the Booking row)
///    happen against the same tracked <see cref="ClassSession"/> and are committed in one
///    <c>SaveChangesAsync</c> call — one transaction.
///  * <see cref="ClassSession.Version"/> is a concurrency token. EF Core adds its original value
///    to the UPDATE ... WHERE clause. If another booking incremented BookedCount in the gap
///    between our read and our save, our UPDATE matches zero rows and EF Core throws
///    <see cref="DbUpdateConcurrencyException"/>, which we translate to a 409.
/// </summary>
public sealed class BookingService : IBookingService
{
    private readonly IClassSessionRepository _sessions;
    private readonly IBookingRepository _bookings;
    private readonly IMapper _mapper;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IClassSessionRepository sessions,
        IBookingRepository bookings,
        IMapper mapper,
        ILogger<BookingService> logger)
    {
        _sessions = sessions;
        _bookings = bookings;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<BookingResponse>> BookAsync(int memberId, CreateBookingRequest request, CancellationToken ct = default)
    {
        var session = await _sessions.GetForUpdateAsync(request.ClassSessionId, ct);
        if (session is null)
            return Result<BookingResponse>.NotFound($"Class session {request.ClassSessionId} was not found.");

        if (session.Status == ClassSessionStatus.Cancelled)
            return Result<BookingResponse>.Validation("This class session has been cancelled.");

        if (session.StartsAtUtc <= DateTime.UtcNow)
            return Result<BookingResponse>.Validation("This class session has already started.");

        var existing = await _bookings.GetAnyAsync(session.Id, memberId, ct);
        if (existing is not null && existing.Status != BookingStatus.CancelledByMember)
        {
            return existing.Status == BookingStatus.Waitlisted
                ? Result<BookingResponse>.Conflict("You are already on the waitlist for this class.")
                : Result<BookingResponse>.Conflict("You already have a booking for this class.");
        }

        var goingToWaitlist = session.IsFull;

        var booking = existing ?? new Booking
        {
            ClassSessionId = session.Id,
            MemberId = memberId
        };
        booking.Status = goingToWaitlist ? BookingStatus.Waitlisted : BookingStatus.Confirmed;
        booking.CreatedAtUtc = DateTime.UtcNow;
        booking.CancelledAtUtc = null;

        int? waitlistPosition = null;

        if (goingToWaitlist)
        {
            var position = await _sessions.NextWaitlistPositionAsync(session.Id, ct);
            waitlistPosition = position;
            await _bookings.AddWaitlistEntryAsync(new WaitlistEntry
            {
                ClassSessionId = session.Id,
                MemberId = memberId,
                Position = position,
                CreatedAtUtc = DateTime.UtcNow
            }, ct);
        }
        else
        {
            // Mutating the session is what arms the concurrency check on save.
            session.BookedCount += 1;
        }

        if (existing is null)
            await _bookings.AddAsync(booking, ct);

        try
        {
            await _bookings.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict: member {MemberId} lost the race for the last spot in session {SessionId}",
                memberId, session.Id);
            return Result<BookingResponse>.Conflict(
                "Someone else took the last spot while you were booking. Please try again.");
        }

        _logger.LogInformation(
            "Booking {Status}: member {MemberId} in session {SessionId} (booked {Booked}/{Capacity})",
            booking.Status, memberId, session.Id, session.BookedCount, session.Capacity);

        var detail = await _bookings.GetDetailAsync(booking.Id, ct);
        var response = _mapper.Map<BookingResponse>(detail!);
        response.WaitlistPosition = waitlistPosition;
        return Result<BookingResponse>.Success(response);
    }

    public async Task<Result> CancelAsync(int memberId, int bookingId, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, ct);
        if (booking is null || booking.MemberId != memberId)
            return Result.NotFound($"Booking {bookingId} was not found.");

        if (booking.Status == BookingStatus.CancelledByMember)
            return Result.Success();

        var wasConfirmed = booking.Status == BookingStatus.Confirmed;
        var wasWaitlisted = booking.Status == BookingStatus.Waitlisted;

        booking.Status = BookingStatus.CancelledByMember;
        booking.CancelledAtUtc = DateTime.UtcNow;

        if (wasWaitlisted)
        {
            var entry = await _bookings.GetWaitlistEntryAsync(booking.ClassSessionId, memberId, ct);
            if (entry is not null)
                _bookings.RemoveWaitlistEntry(entry);
        }

        if (wasConfirmed)
        {
            var session = await _sessions.GetForUpdateAsync(booking.ClassSessionId, ct);
            if (session is not null && session.BookedCount > 0)
                session.BookedCount -= 1;
        }

        try
        {
            await _bookings.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict cancelling booking {BookingId}", bookingId);
            return Result.Conflict("The class was updated while cancelling. Please try again.");
        }

        _logger.LogInformation("Booking {BookingId} cancelled by member {MemberId}", bookingId, memberId);
        return Result.Success();
    }

    public async Task<IReadOnlyList<BookingResponse>> ListForMemberAsync(int memberId, CancellationToken ct = default)
    {
        var bookings = await _bookings.ListForMemberAsync(memberId, ct);
        var mapped = _mapper.Map<IReadOnlyList<BookingResponse>>(bookings);

        // The Booking row does not carry a queue position; it lives on the WaitlistEntry.
        var positions = await _bookings.GetWaitlistPositionsForMemberAsync(memberId, ct);
        foreach (var booking in mapped)
        {
            if (booking.Status == nameof(BookingStatus.Waitlisted)
                && positions.TryGetValue(booking.ClassSessionId, out var position))
            {
                booking.WaitlistPosition = position;
            }
        }

        return mapped;
    }
}
