namespace Gym.Core.Entities;

public class Instructor
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string? Bio { get; set; }

    /// <summary>
    /// False once an admin has deactivated the instructor. Inactive instructors keep their past
    /// sessions, but no new sessions can be scheduled with them.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public ICollection<ClassSession> Sessions { get; set; } = new List<ClassSession>();
}
