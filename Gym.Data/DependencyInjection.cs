using Gym.Core.Interfaces;
using Gym.Data.Repositories;
using Gym.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gym.Data;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the DbContext and repositories. The API project calls this from Program.cs;
    /// it is the only place the API touches the Data layer directly.
    /// </summary>
    public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'ConnectionStrings:Default' is not configured. Set it with: " +
                "dotnet user-secrets set \"ConnectionStrings:Default\" \"Host=...\" --project Gym.API");

        services.AddDbContext<GymDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<IClassTypeRepository, ClassTypeRepository>();
        services.AddScoped<IClassSessionRepository, ClassSessionRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddScoped<DbSeeder>();

        return services;
    }
}
