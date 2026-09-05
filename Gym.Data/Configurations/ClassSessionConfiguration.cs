using Gym.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gym.Data.Configurations;

public sealed class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StartsAtUtc).IsRequired();
        builder.Property(s => s.Capacity).IsRequired();
        builder.Property(s => s.BookedCount).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        // Self-managed optimistic concurrency token (see IConcurrencyStamped / GymDbContext).
        builder.Property(s => s.Version).IsConcurrencyToken();

        builder.HasOne(s => s.Instructor)
            .WithMany(i => i.Sessions)
            .HasForeignKey(s => s.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Bookings)
            .WithOne(b => b.ClassSession)
            .HasForeignKey(b => b.ClassSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.WaitlistEntries)
            .WithOne(w => w.ClassSession)
            .HasForeignKey(w => w.ClassSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.StartsAtUtc);

        // Supports the common "upcoming, still-open sessions" query.
        builder.HasIndex(s => new { s.Status, s.StartsAtUtc }).HasDatabaseName("IX_ClassSessions_Status_StartsAtUtc");
    }
}

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.CreatedAtUtc).IsRequired();

        // A member appears at most once per session (regardless of status history is kept via CancelledAtUtc).
        builder.HasIndex(b => new { b.ClassSessionId, b.MemberId }).IsUnique();
    }
}

public sealed class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.CreatedAtUtc).IsRequired();
        builder.HasIndex(w => new { w.ClassSessionId, w.MemberId }).IsUnique();
    }
}
