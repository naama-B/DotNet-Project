using System.ComponentModel.DataAnnotations;

namespace Gym.Core.DTOs;

public sealed class ClassSessionResponse
{
    public int Id { get; set; }
    public int ClassTypeId { get; set; }
    public string ClassTypeName { get; set; } = default!;
    public string InstructorName { get; set; } = default!;
    public DateTime StartsAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public int AvailableSpots { get; set; }
    public string Status { get; set; } = default!;

    /// <summary>Number of satisfaction ratings members have left for this session.</summary>
    public int RatingCount { get; set; }

    /// <summary>Mean star rating (1–5), rounded to two places; null until the session has been rated.</summary>
    public double? AverageStars { get; set; }
}

public sealed class CreateClassSessionRequest
{
    [Range(1, int.MaxValue)]
    public int ClassTypeId { get; set; }

    [Range(1, int.MaxValue)]
    public int InstructorId { get; set; }

    [Required]
    public DateTime StartsAtUtc { get; set; }

    /// <summary>Optional capacity override. When null the class type's default capacity is used.</summary>
    [Range(1, 500)]
    public int? Capacity { get; set; }
}

/// <summary>Query-string parameters for the paged session list. Bound with <c>[FromQuery]</c>.</summary>
public sealed class ClassSessionQueryParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;
    private int _page = 1;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > MaxPageSize ? 20 : value;
    }

    /// <summary>Free-text match against class type name.</summary>
    public string? Search { get; set; }

    /// <summary>Only sessions starting at or after this instant (UTC).</summary>
    public DateTime? FromUtc { get; set; }

    /// <summary>When true, hide sessions that are already full.</summary>
    public bool? OnlyAvailable { get; set; }

    /// <summary>"startsAt" (default) or "available".</summary>
    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
}
