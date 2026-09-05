using Gym.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gym.Data.Configurations;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.FullName).IsRequired().HasMaxLength(120);
        builder.Property(m => m.Email).IsRequired().HasMaxLength(200);
        builder.Property(m => m.PasswordHash).IsRequired().HasMaxLength(255);
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(m => m.Email).IsUnique();

        builder.HasMany(m => m.Bookings)
            .WithOne(b => b.Member)
            .HasForeignKey(b => b.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.WaitlistEntries)
            .WithOne(w => w.Member)
            .HasForeignKey(w => w.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
