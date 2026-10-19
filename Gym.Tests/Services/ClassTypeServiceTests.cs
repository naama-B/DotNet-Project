using Gym.Core.Common;
using Gym.Core.Entities;
using Gym.Core.Interfaces;
using Gym.Service.Services;
using Gym.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Gym.Tests.Services;

public sealed class ClassTypeServiceTests
{
    private readonly Mock<IClassTypeRepository> _classTypes = new(MockBehavior.Strict);

    private ClassTypeService CreateSut() =>
        new(_classTypes.Object, TestMapper.Create(), NullLogger<ClassTypeService>.Instance);

    [Fact]
    public async Task DeactivateAsync_is_refused_while_upcoming_sessions_exist()
    {
        var entity = new ClassType { Id = 1, Name = "Pilates", IsActive = true };
        _classTypes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _classTypes.Setup(r => r.HasUpcomingSessionsAsync(1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut().DeactivateAsync(1);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_marks_the_type_inactive_when_no_upcoming_sessions()
    {
        var entity = new ClassType { Id = 1, Name = "Pilates", IsActive = true };
        _classTypes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _classTypes.Setup(r => r.HasUpcomingSessionsAsync(1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _classTypes.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().DeactivateAsync(1);

        Assert.True(result.IsSuccess);
        Assert.False(entity.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_returns_NotFound_for_an_unknown_type()
    {
        _classTypes.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((ClassType?)null);

        var result = await CreateSut().DeactivateAsync(99);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ActivateAsync_marks_an_inactive_type_active()
    {
        var entity = new ClassType { Id = 1, Name = "Pilates", IsActive = false };
        _classTypes.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _classTypes.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().ActivateAsync(1);

        Assert.True(result.IsSuccess);
        Assert.True(entity.IsActive);
    }
}
