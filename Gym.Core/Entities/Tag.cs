namespace Gym.Core.Entities;

/// <summary>A free-form label attached to class types (e.g. "Cardio", "Beginner"). Many-to-many with <see cref="ClassType"/>.</summary>
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    public ICollection<ClassType> ClassTypes { get; set; } = new List<ClassType>();
}
