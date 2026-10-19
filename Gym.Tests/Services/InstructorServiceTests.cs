using Gym.Core.Common;
using Gym.Core.Entities;
using Gym.Core.Interfaces;
using Gym.Service.Services;
using Gym.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Gym.Tests.Services;

public sealed class InstructorServiceTests
{
    private readonly Mock<IInstructorRepository> _instructors = new(MockBehavior.Strict);

    private InstructorService CreateSut() =>
        new(_instructors.Object, TestMapper.Create(), NullLogger<InstructorService>.Instance);

    [Fact]
    public async Task DeactivateAsync_is_refused_while_upcoming_sessions_exist()
    {
        var entity = new Instructor { Id = 1, FullName = "Dana", IsActive = true };
        _instructors.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _instructors.Setup(r => r.HasUpcomingSessionsAsync(1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut().DeactivateAsync(1);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_marks_the_instructor_inactive_when_no_upcoming_sessions()
    {
        var entity = new Instructor { Id = 1, FullName = "Dana", IsActive = true };
        _instructors.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _instructors.Setup(r => r.HasUpcomingSessionsAsync(1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _instructors.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().DeactivateAsync(1);

        Assert.True(result.IsSuccess);
        Assert.False(entity.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_returns_NotFound_for_an_unknown_instructor()
    {
        _instructors.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Instructor?)null);

        var result = await CreateSut().DeactivateAsync(99);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ActivateAsync_marks_an_inactive_instructor_active()
    {
        var entity = new Instructor { Id = 1, FullName = "Dana", IsActive = false };
        _instructors.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _instructors.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().ActivateAsync(1);

        Assert.True(result.IsSuccess);
        Assert.True(entity.IsActive);
    }
}
