using System.ComponentModel.DataAnnotations;

namespace Gym.Core.DTOs;

public sealed class InstructorResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string? Bio { get; set; }
}

public sealed class CreateInstructorRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = default!;

    [StringLength(1000)]
    public string? Bio { get; set; }
}
