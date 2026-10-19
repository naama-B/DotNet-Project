namespace Gym.Core.Entities;

/// <summary>A kind of class offered by the gym (e.g. "Classic Aerobics", "Pilates"). One-to-many with <see cref="ClassSession"/>.</summary>
public class ClassType
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }

    /// <summary>Capacity applied to a new session unless the caller overrides it.</summary>
    public int DefaultCapacity { get; set; }

    /// <summary>
    /// False once an admin has deactivated the class type. Inactive types keep their past
    /// sessions, bookings and ratings, but no new sessions can be scheduled for them.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public ICollection<ClassSession> Sessions { get; set; } = new List<ClassSession>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
