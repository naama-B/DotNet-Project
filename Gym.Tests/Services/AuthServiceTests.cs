using Gym.Core.DTOs;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Gym.Service.Security;
using Gym.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Gym.Tests.Services;

public sealed class AuthServiceTests
{
    private readonly Mock<IMemberRepository> _members = new();
    private readonly IPasswordHasher _hasher = new BCryptPasswordHasher();
    private readonly Mock<IJwtTokenService> _tokens = new();

    private AuthService CreateSut()
    {
        _tokens.Setup(t => t.CreateToken(It.IsAny<Member>()))
            .Returns(("stub-token", DateTime.UtcNow.AddHours(1)));
        return new AuthService(_members.Object, _hasher, _tokens.Object, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_rejects_a_duplicate_email()
    {
        _members.Setup(r => r.EmailExistsAsync("taken@gym.local", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateSut().RegisterAsync(new RegisterRequest
        {
            FullName = "Dana", Email = "taken@gym.local", Password = "secret123"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(Gym.Core.Common.ResultStatus.Conflict, result.Status);
        _members.Verify(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_persists_and_returns_a_token()
    {
        _members.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _members.Setup(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _members.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().RegisterAsync(new RegisterRequest
        {
            FullName = "  Dana  ", Email = "  Dana@Gym.Local ", Password = "secret123"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("dana@gym.local", result.Value!.Email);
        Assert.Equal("Dana", result.Value.FullName);
        Assert.Equal("stub-token", result.Value.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_rejects_a_wrong_password()
    {
        var member = new Member
        {
            Id = 1, Email = "dana@gym.local", FullName = "Dana",
            Role = UserRole.Member, PasswordHash = _hasher.Hash("correct-password")
        };
        _members.Setup(r => r.GetByEmailAsync("dana@gym.local", It.IsAny<CancellationToken>())).ReturnsAsync(member);

        var result = await CreateSut().LoginAsync(new LoginRequest { Email = "dana@gym.local", Password = "wrong" });

        Assert.False(result.IsSuccess);
        Assert.Equal(Gym.Core.Common.ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task LoginAsync_succeeds_with_the_right_password()
    {
        var member = new Member
        {
            Id = 1, Email = "dana@gym.local", FullName = "Dana",
            Role = UserRole.Member, PasswordHash = _hasher.Hash("correct-password")
        };
        _members.Setup(r => r.GetByEmailAsync("dana@gym.local", It.IsAny<CancellationToken>())).ReturnsAsync(member);

        var result = await CreateSut().LoginAsync(new LoginRequest { Email = "dana@gym.local", Password = "correct-password" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Member", result.Value!.Role);
    }
}
