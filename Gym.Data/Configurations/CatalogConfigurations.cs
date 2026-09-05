using Gym.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gym.Data.Configurations;

public sealed class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.FullName).IsRequired().HasMaxLength(120);
        builder.Property(i => i.Bio).HasMaxLength(1000);
    }
}

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(40);
        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public sealed class ClassTypeConfiguration : IEntityTypeConfiguration<ClassType>
{
    public void Configure(EntityTypeBuilder<ClassType> builder)
    {
        builder.HasKey(ct => ct.Id);
        builder.Property(ct => ct.Name).IsRequired().HasMaxLength(80);
        builder.Property(ct => ct.Description).HasMaxLength(500);
        builder.HasIndex(ct => ct.Name).IsUnique();

        // one-to-many: a class type has many scheduled sessions
        builder.HasMany(ct => ct.Sessions)
            .WithOne(s => s.ClassType)
            .HasForeignKey(s => s.ClassTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // many-to-many: class types tagged with tags, through an implicit join table
        builder.HasMany(ct => ct.Tags)
            .WithMany(t => t.ClassTypes)
            .UsingEntity(join => join.ToTable("ClassTypeTags"));
    }
}
