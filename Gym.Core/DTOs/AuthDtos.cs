using System.ComponentModel.DataAnnotations;

namespace Gym.Core.DTOs;

public sealed class RegisterRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = default!;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = default!;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = default!;
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;
}

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public int MemberId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;
}
