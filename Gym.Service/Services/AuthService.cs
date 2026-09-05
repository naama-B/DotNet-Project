using Gym.Core.Common;
using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gym.Service.Services;

public sealed class AuthService : IAuthService
{
    private readonly IMemberRepository _members;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokens;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IMemberRepository members,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokens,
        ILogger<AuthService> logger)
    {
        _members = members;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _members.EmailExistsAsync(email, ct))
            return Result<AuthResponse>.Conflict("An account with this email already exists.");

        var member = new Member
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Member,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _members.AddAsync(member, ct);
        await _members.SaveChangesAsync(ct);

        _logger.LogInformation("New member registered: {MemberId}", member.Id);
        return Result<AuthResponse>.Success(BuildResponse(member));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var member = await _members.GetByEmailAsync(email, ct);

        if (member is null || !_passwordHasher.Verify(request.Password, member.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", email);
            return Result<AuthResponse>.Unauthorized("Invalid email or password.");
        }

        return Result<AuthResponse>.Success(BuildResponse(member));
    }

    private AuthResponse BuildResponse(Member member)
    {
        var (token, expiresAt) = _tokens.CreateToken(member);
        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAt,
            MemberId = member.Id,
            FullName = member.FullName,
            Email = member.Email,
            Role = member.Role.ToString()
        };
    }
}
