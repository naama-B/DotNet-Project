using System.ComponentModel.DataAnnotations;

namespace Gym.Core.DTOs;

public sealed class CreateBookingRequest
{
    [Range(1, int.MaxValue)]
    public int ClassSessionId { get; set; }
}

public sealed class BookingResponse
{
    public int Id { get; set; }
    public int ClassSessionId { get; set; }
    public string ClassTypeName { get; set; } = default!;
    public DateTime StartsAtUtc { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int? WaitlistPosition { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
