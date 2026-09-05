using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gym.Data;

/// <summary>
/// Used only by the <c>dotnet ef</c> tooling (migrations add / script). Runtime configuration
/// lives in <c>Gym.API/Program.cs</c>. The connection string here is never used to talk to a
/// real database during "migrations add" — EF only needs the provider to emit the right SQL.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GymDbContext>
{
    public GymDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("GYM_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=gym_design;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<GymDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new GymDbContext(options);
    }
}
