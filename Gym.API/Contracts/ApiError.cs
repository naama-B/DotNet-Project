namespace Gym.API.Contracts;

/// <summary>The single JSON error shape every failing endpoint returns.</summary>
public sealed class ApiError
{
    public int Status { get; set; }
    public string Title { get; set; } = default!;
    public string? Detail { get; set; }
    public string? CorrelationId { get; set; }
}
