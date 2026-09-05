namespace Gym.Core.Entities;

/// <summary>A member queued for a full <see cref="ClassSession"/>, ordered by <see cref="Position"/>.</summary>
public class WaitlistEntry
{
    public int Id { get; set; }

    public int ClassSessionId { get; set; }
    public ClassSession ClassSession { get; set; } = default!;

    public int MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public int Position { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
