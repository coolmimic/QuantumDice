using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantumDice.Core.Entities;

namespace QuantumDice.Infrastructure.Data.Configurations;

public class GameRoundConfiguration : IEntityTypeConfiguration<GameRound>
{
    public void Configure(EntityTypeBuilder<GameRound> builder)
    {
        builder.ToTable("game_rounds");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RoundNumber).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        
        builder.HasIndex(e => new { e.GroupId, e.GameTypeId, e.RoundNumber }).IsUnique();
        
        builder.HasOne(e => e.Group)
            .WithMany(g => g.Rounds)
            .HasForeignKey(e => e.GroupId);
            
        builder.HasOne(e => e.GameType)
            .WithMany(g => g.Rounds)
            .HasForeignKey(e => e.GameTypeId);
    }
}

public class DiceResultConfiguration : IEntityTypeConfiguration<DiceResult>
{
    public void Configure(EntityTypeBuilder<DiceResult> builder)
    {
        builder.ToTable("dice_results");
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => new { e.RoundId, e.DiceIndex }).IsUnique();
        
        builder.HasOne(e => e.Round)
            .WithMany(r => r.DiceResults)
            .HasForeignKey(e => e.RoundId);
    }
}

public class BetConfiguration : IEntityTypeConfiguration<Bet>
{
    public void Configure(EntityTypeBuilder<Bet> builder)
    {
        builder.ToTable("bets");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.BetContent).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ParsedContent).HasColumnType("jsonb");
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Odds).HasPrecision(10, 2);
        builder.Property(e => e.WinAmount).HasPrecision(18, 2);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        
        builder.HasOne(e => e.Player)
            .WithMany(p => p.Bets)
            .HasForeignKey(e => e.PlayerId);
            
        builder.HasOne(e => e.Round)
            .WithMany(r => r.Bets)
            .HasForeignKey(e => e.RoundId);
            
        builder.HasOne(e => e.BetMethod)
            .WithMany(b => b.Bets)
            .HasForeignKey(e => e.BetMethodId);
    }
}
