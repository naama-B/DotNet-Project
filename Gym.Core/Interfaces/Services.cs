using Gym.Core.Common;
using Gym.Core.DTOs;
using Gym.Core.Entities;

namespace Gym.Core.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public interface IClassTypeService
{
    Task<IReadOnlyList<ClassTypeResponse>> ListAsync(CancellationToken ct = default);
    Task<Result<ClassTypeResponse>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<ClassTypeResponse>> CreateAsync(CreateClassTypeRequest request, CancellationToken ct = default);
    Task<Result<ClassTypeResponse>> UpdateAsync(int id, UpdateClassTypeRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IInstructorService
{
    Task<IReadOnlyList<InstructorResponse>> ListAsync(CancellationToken ct = default);
    Task<Result<InstructorResponse>> CreateAsync(CreateInstructorRequest request, CancellationToken ct = default);
}

public interface IClassSessionService
{
    Task<PagedResult<ClassSessionResponse>> ListAsync(ClassSessionQueryParameters parameters, CancellationToken ct = default);
    Task<Result<ClassSessionResponse>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<ClassSessionResponse>> CreateAsync(CreateClassSessionRequest request, CancellationToken ct = default);
    Task<Result> CancelAsync(int id, CancellationToken ct = default);

    /// <summary>The waiting list for a session, ordered by queue position.</summary>
    Task<Result<SessionWaitlistResponse>> GetWaitlistAsync(int id, CancellationToken ct = default);
}

public interface IClassRatingService
{
    /// <summary>Every satisfaction rating for a session, with the average.</summary>
    Task<Result<SessionRatingsResponse>> ListForSessionAsync(int classSessionId, CancellationToken ct = default);

    /// <summary>
    /// Every session members have rated, each with its title, instructor and the ratings
    /// themselves (newest first). Sessions with no ratings are omitted.
    /// </summary>
    Task<IReadOnlyList<ReviewedSessionResponse>> ListReviewedSessionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Records (or updates) <paramref name="memberId"/>'s star rating for a session. Only a
    /// member who held a confirmed booking for a session that has already taken place may rate it.
    /// </summary>
    Task<Result<ClassRatingResponse>> RateAsync(int memberId, int classSessionId, CreateClassRatingRequest request, CancellationToken ct = default);
}

public interface IBookingService
{
    /// <summary>
    /// Books <paramref name="memberId"/> into the session. Business rules and the save run in
    /// one transaction; a concurrent booking that wins the race makes this one fail with
    /// <see cref="ResultStatus.Conflict"/>.
    /// </summary>
    Task<Result<BookingResponse>> BookAsync(int memberId, CreateBookingRequest request, CancellationToken ct = default);

    Task<Result> CancelAsync(int memberId, int bookingId, CancellationToken ct = default);

    Task<IReadOnlyList<BookingResponse>> ListForMemberAsync(int memberId, CancellationToken ct = default);
}

/// <summary>Issues signed JWTs for authenticated members.</summary>
public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(Member member);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
