using AutoMapper;
using Gym.Service.Mapping;

namespace Gym.Tests.TestSupport;

/// <summary>Builds a real <see cref="IMapper"/> from the production profile, so mapping is exercised, not stubbed.</summary>
public static class TestMapper
{
    public static IMapper Create()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }
}
