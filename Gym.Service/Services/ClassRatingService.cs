using AutoMapper;
using Gym.Core.Common;
using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gym.Service.Services;

/// <summary>
/// Satisfaction ratings for a <see cref="ClassSession"/>. A rating is only accepted from a
/// member who held a confirmed booking for a session that has already taken place, and each
/// member has at most one rating per session (a second POST overwrites the first).
/// </summary>
public sealed class ClassRatingService : IClassRatingService
{
    private readonly IClassRatingRepository _ratings;
    private readonly IClassSessionRepository _sessions;
    private readonly IBookingRepository _bookings;
    private readonly IMapper _mapper;
    private readonly ILogger<ClassRatingService> _logger;

    public ClassRatingService(
        IClassRatingRepository ratings,
        IClassSessionRepository sessions,
        IBookingRepository bookings,
        IMapper mapper,
        ILogger<ClassRatingService> logger)
    {
        _ratings = ratings;
        _sessions = sessions;
        _bookings = bookings;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<SessionRatingsResponse>> ListForSessionAsync(int classSessionId, CancellationToken ct = default)
    {
        var session = await _sessions.GetDetailAsync(classSessionId, ct);
        if (session is null)
            return Result<SessionRatingsResponse>.NotFound($"Class session {classSessionId} was not found.");

        var ratings = await _ratings.ListForSessionAsync(classSessionId, ct);

        return Result<SessionRatingsResponse>.Success(new SessionRatingsResponse
        {
            ClassSessionId = session.Id,
            ClassTypeName = session.ClassType.Name,
            RatingCount = ratings.Count,
            AverageStars = ratings.Count > 0 ? Math.Round(ratings.Average(r => r.Stars), 2) : null,
            Ratings = _mapper.Map<IReadOnlyList<ClassRatingResponse>>(ratings)
        });
    }

    public async Task<IReadOnlyList<ReviewedSessionResponse>> ListReviewedSessionsAsync(CancellationToken ct = default)
    {
        var sessions = await _sessions.ListRatedAsync(ct);

        return sessions.Select(s => new ReviewedSessionResponse
        {
            ClassSessionId = s.Id,
            ClassTypeName = s.ClassType.Name,
            InstructorName = s.Instructor.FullName,
            StartsAtUtc = s.StartsAtUtc,
            RatingCount = s.Ratings.Count,
            AverageStars = s.Ratings.Count > 0 ? Math.Round(s.Ratings.Average(r => r.Stars), 2) : null,
            Ratings = _mapper.Map<IReadOnlyList<ClassRatingResponse>>(
                s.Ratings.OrderByDescending(r => r.CreatedAtUtc).ToList())
        }).ToList();
    }

    public async Task<Result<ClassRatingResponse>> RateAsync(
        int memberId, int classSessionId, CreateClassRatingRequest request, CancellationToken ct = default)
    {
        var session = await _sessions.GetDetailAsync(classSessionId, ct);
        if (session is null)
            return Result<ClassRatingResponse>.NotFound($"Class session {classSessionId} was not found.");

        if (session.StartsAtUtc > DateTime.UtcNow)
            return Result<ClassRatingResponse>.Validation("You can only rate a class after it has taken place.");

        var booking = await _bookings.GetAnyAsync(classSessionId, memberId, ct);
        if (booking is null || booking.Status != BookingStatus.Confirmed)
            return Result<ClassRatingResponse>.Forbidden("Only members who attended this class can rate it.");

        var now = DateTime.UtcNow;
        var rating = await _ratings.GetForMemberAsync(classSessionId, memberId, ct);
        var isNew = rating is null;

        if (rating is null)
        {
            rating = new ClassRating
            {
                ClassSessionId = classSessionId,
                MemberId = memberId,
                CreatedAtUtc = now
            };
            await _ratings.AddAsync(rating, ct);
        }
        else
        {
            rating.UpdatedAtUtc = now;
        }

        rating.Stars = request.Stars;
        rating.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        await _ratings.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Rating {Action}: member {MemberId} gave session {SessionId} {Stars} star(s)",
            isNew ? "created" : "updated", memberId, classSessionId, rating.Stars);

        var saved = await _ratings.GetForMemberAsync(classSessionId, memberId, ct);
        return Result<ClassRatingResponse>.Success(_mapper.Map<ClassRatingResponse>(saved!));
    }
}
