using Gym.Core.Interfaces;
using Gym.Service.Mapping;
using Gym.Service.Options;
using Gym.Service.Security;
using Gym.Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Gym.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();


        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IInstructorService, InstructorService>();
        services.AddScoped<IClassTypeService, ClassTypeService>();
        services.AddScoped<IClassSessionService, ClassSessionService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IClassRatingService, ClassRatingService>();

        return services;
    }
}
