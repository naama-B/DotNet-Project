namespace Gym.Core.Entities;

/// <summary>
/// A member's satisfaction rating for a <see cref="ClassSession"/> they attended: 1–5 stars
/// and an optional free-text comment. One row per member per session (see the unique index).
/// </summary>
public class ClassRating
{
    public int Id { get; set; }

    public int ClassSessionId { get; set; }
    public ClassSession ClassSession { get; set; } = default!;

    public int MemberId { get; set; }
    public Member Member { get; set; } = default!;

    /// <summary>Satisfaction from 1 (poor) to 5 (excellent).</summary>
    public int Stars { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Set when the member later changes their rating.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
}
