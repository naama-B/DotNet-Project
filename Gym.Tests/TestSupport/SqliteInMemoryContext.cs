using Gym.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Gym.Tests.TestSupport;

/// <summary>
/// A throwaway SQLite database that lives in memory for the lifetime of the connection.
/// Two <see cref="GymDbContext"/> instances built from the same factory share the one database,
/// which is exactly what the optimistic-concurrency proof needs.
/// </summary>
public sealed class SqliteInMemoryContext : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<GymDbContext> _options;

    public SqliteInMemoryContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<GymDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }

    public GymDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
