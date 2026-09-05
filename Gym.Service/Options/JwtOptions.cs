using System.ComponentModel.DataAnnotations;

namespace Gym.Service.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = default!;
    [Required] public string Audience { get; set; } = default!;

    /// <summary>Signing key. Never committed; supplied via user-secrets / environment.</summary>
    [Required, MinLength(32)]
    public string Key { get; set; } = default!;

    [Range(5, 1440)]
    public int AccessTokenMinutes { get; set; } = 120;
}
