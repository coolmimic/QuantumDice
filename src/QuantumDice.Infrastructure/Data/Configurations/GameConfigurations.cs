using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantumDice.Core.Entities;

namespace QuantumDice.Infrastructure.Data.Configurations;

public class GameTypeConfiguration : IEntityTypeConfiguration<GameType>
{
    public void Configure(EntityTypeBuilder<GameType> builder)
    {
        builder.ToTable("game_types");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();
        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
    }
}

public class BaseGameConfiguration : IEntityTypeConfiguration<BaseGame>
{
    public void Configure(EntityTypeBuilder<BaseGame> builder)
    {
        builder.ToTable("base_games");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();
        
        builder.HasIndex(e => new { e.GameTypeId, e.Code }).IsUnique();
        
        builder.HasOne(e => e.GameType)
            .WithMany(g => g.BaseGames)
            .HasForeignKey(e => e.GameTypeId);
    }
}

public class BetMethodConfiguration : IEntityTypeConfiguration<BetMethod>
{
    public void Configure(EntityTypeBuilder<BetMethod> builder)
    {
        builder.ToTable("bet_methods");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();
        builder.Property(e => e.BetPattern).HasMaxLength(100);
        builder.Property(e => e.DefaultOdds).HasPrecision(10, 2);
        builder.Property(e => e.Description).HasMaxLength(500);
        
        builder.HasIndex(e => new { e.BaseGameId, e.Code }).IsUnique();
        
        builder.HasOne(e => e.BaseGame)
            .WithMany(b => b.BetMethods)
            .HasForeignKey(e => e.BaseGameId);
    }
}
