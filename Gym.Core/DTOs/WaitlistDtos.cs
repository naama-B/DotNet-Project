namespace Gym.Core.DTOs;

/// <summary>One member's place in a session's waiting list.</summary>
public sealed class WaitlistEntryResponse
{
    public int Position { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>The full waiting list for a session, ordered by <see cref="WaitlistEntryResponse.Position"/>.</summary>
public sealed class SessionWaitlistResponse
{
    public int ClassSessionId { get; set; }
    public string ClassTypeName { get; set; } = default!;
    public DateTime StartsAtUtc { get; set; }
    public int Count { get; set; }
    public IReadOnlyList<WaitlistEntryResponse> Entries { get; set; } = Array.Empty<WaitlistEntryResponse>();
}
