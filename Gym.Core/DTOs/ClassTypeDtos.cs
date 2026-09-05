using System.ComponentModel.DataAnnotations;

namespace Gym.Core.DTOs;

public sealed class ClassTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int DefaultCapacity { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
}

public sealed class CreateClassTypeRequest
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(10, 240)]
    public int DurationMinutes { get; set; }

    [Range(1, 500)]
    public int DefaultCapacity { get; set; }

    /// <summary>Tag names; unknown names are created on the fly.</summary>
    public List<string> Tags { get; set; } = new();
}

public sealed class UpdateClassTypeRequest
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(10, 240)]
    public int DurationMinutes { get; set; }

    [Range(1, 500)]
    public int DefaultCapacity { get; set; }

    public List<string> Tags { get; set; } = new();
}
