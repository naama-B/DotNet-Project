using Gym.Core.Enums;

namespace Gym.Core.Entities;

public class Booking
{
    public int Id { get; set; }

    public int ClassSessionId { get; set; }
    public ClassSession ClassSession { get; set; } = default!;

    public int MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}
