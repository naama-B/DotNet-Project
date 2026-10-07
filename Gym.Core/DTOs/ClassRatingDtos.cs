using System.ComponentModel.DataAnnotations;

namespace Gym.Core.DTOs;

/// <summary>A member's 1–5 star satisfaction rating for a session. Re-posting overwrites the previous one.</summary>
public sealed class CreateClassRatingRequest
{
    [Range(1, 5)]
    public int Stars { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

public sealed class ClassRatingResponse
{
    public int Id { get; set; }
    public int ClassSessionId { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = default!;
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>Every rating for a session plus the aggregate members see before booking.</summary>
public sealed class SessionRatingsResponse
{
    public int ClassSessionId { get; set; }
    public string ClassTypeName { get; set; } = default!;
    public int RatingCount { get; set; }

    /// <summary>Mean of <see cref="ClassRatingResponse.Stars"/>, rounded to two places; null when there are no ratings.</summary>
    public double? AverageStars { get; set; }

    public IReadOnlyList<ClassRatingResponse> Ratings { get; set; } = Array.Empty<ClassRatingResponse>();
}

/// <summary>
/// One session that members have rated: its title and instructor, the aggregate score, and
/// every rating (newest first). Backs the standalone reviews page.
/// </summary>
public sealed class ReviewedSessionResponse
{
    public int ClassSessionId { get; set; }
    public string ClassTypeName { get; set; } = default!;
    public string InstructorName { get; set; } = default!;
    public DateTime StartsAtUtc { get; set; }
    public int RatingCount { get; set; }

    /// <summary>Mean star score, rounded to two places.</summary>
    public double? AverageStars { get; set; }

    public IReadOnlyList<ClassRatingResponse> Ratings { get; set; } = Array.Empty<ClassRatingResponse>();
}
