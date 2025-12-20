using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantumDice.Core.Entities;

namespace QuantumDice.Infrastructure.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TelegramGroupId).IsUnique();
        builder.Property(e => e.GroupName).HasMaxLength(200);
        
        builder.HasOne(e => e.Dealer)
            .WithMany(d => d.Groups)
            .HasForeignKey(e => e.DealerId);
    }
}

public class GroupGameConfigConfiguration : IEntityTypeConfiguration<GroupGameConfig>
{
    public void Configure(EntityTypeBuilder<GroupGameConfig> builder)
    {
        builder.ToTable("group_game_configs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CustomOdds).HasPrecision(10, 2);
        builder.Property(e => e.MinBet).HasPrecision(10, 2);
        builder.Property(e => e.MaxBet).HasPrecision(10, 2);
        
        builder.HasIndex(e => new { e.GroupId, e.BetMethodId }).IsUnique();
        
        builder.HasOne(e => e.Group)
            .WithMany(g => g.GameConfigs)
            .HasForeignKey(e => e.GroupId);
            
        builder.HasOne(e => e.BetMethod)
            .WithMany(b => b.GroupConfigs)
            .HasForeignKey(e => e.BetMethodId);
    }
}

public class GroupScheduleConfigConfiguration : IEntityTypeConfiguration<GroupScheduleConfig>
{
    public void Configure(EntityTypeBuilder<GroupScheduleConfig> builder)
    {
        builder.ToTable("group_schedule_configs");
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => new { e.GroupId, e.GameTypeId }).IsUnique();
        
        builder.HasOne(e => e.Group)
            .WithMany(g => g.ScheduleConfigs)
            .HasForeignKey(e => e.GroupId);
            
        builder.HasOne(e => e.GameType)
            .WithMany(g => g.ScheduleConfigs)
            .HasForeignKey(e => e.GameTypeId);
    }
}
