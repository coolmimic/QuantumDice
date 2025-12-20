using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantumDice.Core.Entities;

namespace QuantumDice.Infrastructure.Data.Configurations;

public class DealerConfiguration : IEntityTypeConfiguration<Dealer>
{
    public void Configure(EntityTypeBuilder<Dealer> builder)
    {
        builder.ToTable("dealers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Username).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Username).IsUnique();
        builder.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(e => e.ContactTelegram).HasMaxLength(100);
        
        builder.HasOne(e => e.Creator)
            .WithMany(s => s.CreatedDealers)
            .HasForeignKey(e => e.CreatedBy);
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasPrecision(10, 2);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        
        builder.HasOne(e => e.Dealer)
            .WithMany(d => d.Subscriptions)
            .HasForeignKey(e => e.DealerId);
            
        builder.HasOne(e => e.Creator)
            .WithMany(s => s.CreatedSubscriptions)
            .HasForeignKey(e => e.CreatedBy);
    }
}
