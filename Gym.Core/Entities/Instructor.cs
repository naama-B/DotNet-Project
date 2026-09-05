namespace Gym.Core.Entities;

public class Instructor
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string? Bio { get; set; }

    public ICollection<ClassSession> Sessions { get; set; } = new List<ClassSession>();
}
