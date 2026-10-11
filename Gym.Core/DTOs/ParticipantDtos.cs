namespace Gym.Core.DTOs;

/// <summary>A member who holds a confirmed spot in a session.</summary>
public sealed class ParticipantResponse
{
    public int BookingId { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public DateTime BookedAtUtc { get; set; }
}

/// <summary>Everyone booked into a session, in booking order.</summary>
public sealed class SessionParticipantsResponse
{
    public int ClassSessionId { get; set; }
    public string ClassTypeName { get; set; } = default!;
    public DateTime StartsAtUtc { get; set; }
    public int Capacity { get; set; }
    public int Count { get; set; }
    public IReadOnlyList<ParticipantResponse> Participants { get; set; } = Array.Empty<ParticipantResponse>();
}
