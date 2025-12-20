using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantumDice.Core.Entities;

namespace QuantumDice.Infrastructure.Data.Configurations;

public class SuperAdminConfiguration : IEntityTypeConfiguration<SuperAdmin>
{
    public void Configure(EntityTypeBuilder<SuperAdmin> builder)
    {
        builder.ToTable("super_admins");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Username).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Username).IsUnique();
        builder.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
    }
}

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("system_configs");
        builder.HasKey(e => e.Key);
        builder.Property(e => e.Key).HasMaxLength(100);
        builder.Property(e => e.Value).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
    }
}
