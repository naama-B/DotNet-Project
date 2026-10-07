using Gym.Core.Common;
using Gym.Core.Entities;
using Gym.Core.Enums;
using Gym.Core.Interfaces;
using Gym.Service.Services;
using Gym.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Gym.Tests.Services;

public sealed class ClassSessionServiceTests
{
    private readonly Mock<IClassSessionRepository> _sessions = new(MockBehavior.Strict);
    private readonly Mock<IClassTypeRepository> _classTypes = new(MockBehavior.Strict);
    private readonly Mock<IInstructorRepository> _instructors = new(MockBehavior.Strict);

    private ClassSessionService CreateSut() =>
        new(_sessions.Object, _classTypes.Object, _instructors.Object, TestMapper.Create(), NullLogger<ClassSessionService>.Instance);

    [Fact]
    public async Task GetWaitlistAsync_returns_entries_in_queue_order()
    {
        var session = new ClassSession
        {
            Id = 1,
            StartsAtUtc = DateTime.UtcNow.AddDays(1),
            ClassType = new ClassType { Id = 1, Name = "Spin", DurationMinutes = 45 }
        };
        _sessions.Setup(r => r.GetDetailAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _sessions.Setup(r => r.GetWaitlistAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WaitlistEntry>
            {
                new() { Position = 1, MemberId = 7, Member = new Member { Id = 7, FullName = "Dana" }, CreatedAtUtc = DateTime.UtcNow },
                new() { Position = 2, MemberId = 8, Member = new Member { Id = 8, FullName = "Noa" }, CreatedAtUtc = DateTime.UtcNow },
            });

        var result = await CreateSut().GetWaitlistAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Spin", result.Value!.ClassTypeName);
        Assert.Equal(2, result.Value.Count);
        Assert.Collection(result.Value.Entries,
            e => Assert.Equal("Dana", e.MemberName),
            e => Assert.Equal("Noa", e.MemberName));
    }

    [Fact]
    public async Task GetWaitlistAsync_returns_NotFound_for_an_unknown_session()
    {
        _sessions.Setup(r => r.GetDetailAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((ClassSession?)null);

        var result = await CreateSut().GetWaitlistAsync(99);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
