using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Gym.Core.Enums;

namespace Gym.Core.Entities;

/// <summary>
/// A single scheduled run of a <see cref="ClassType"/> at a point in time.
/// This is the limited resource members compete for: it has a fixed <see cref="Capacity"/>,
/// and two members booking the last free spot at the same instant must not both succeed.
/// </summary>
public class ClassSession : IConcurrencyStamped
{
    public int Id { get; set; }

    public int ClassTypeId { get; set; }
    public ClassType ClassType { get; set; } = default!;

    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = default!;

    public DateTime StartsAtUtc { get; set; }

    /// <summary>Maximum number of confirmed bookings.</summary>
    public int Capacity { get; set; }

    /// <summary>Number of currently confirmed bookings. Kept in step with <see cref="Bookings"/> by the service layer.</summary>
    public int BookedCount { get; set; }

    public ClassSessionStatus Status { get; set; } = ClassSessionStatus.Scheduled;

    /// <summary>
    /// Self-managed optimistic-concurrency token. Refreshed on every save; its original value
    /// goes into the UPDATE WHERE clause so a stale write updates zero rows.
    /// </summary>
    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<WaitlistEntry> WaitlistEntries { get; set; } = new List<WaitlistEntry>();

    [NotMapped]
    public int AvailableSpots => Math.Max(0, Capacity - BookedCount);

    [NotMapped]
    public bool IsFull => BookedCount >= Capacity;
}
