namespace Gym.Core.Entities;

/// <summary>A kind of class offered by the gym (e.g. "Spin", "Yoga"). One-to-many with <see cref="ClassSession"/>.</summary>
public class ClassType
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }

    /// <summary>Capacity applied to a new session unless the caller overrides it.</summary>
    public int DefaultCapacity { get; set; }

    public ICollection<ClassSession> Sessions { get; set; } = new List<ClassSession>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
