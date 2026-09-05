using Gym.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gym.Data;

public class GymDbContext : DbContext
{
    public GymDbContext(DbContextOptions<GymDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<ClassType> ClassTypes => Set<ClassType>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GymDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampConcurrencyTokens();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampConcurrencyTokens();
        return base.SaveChanges();
    }

    /// <summary>
    /// Refreshes the <see cref="IConcurrencyStamped.Version"/> of every entity that is being
    /// added or modified. EF Core keeps the <i>original</i> value and puts it in the UPDATE
    /// WHERE clause, so a write based on a stale read matches no row and raises
    /// <see cref="DbUpdateConcurrencyException"/>. Doing it here means no service has to remember to.
    /// </summary>
    private void StampConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<IConcurrencyStamped>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.Version = Guid.NewGuid();
        }
    }
}
