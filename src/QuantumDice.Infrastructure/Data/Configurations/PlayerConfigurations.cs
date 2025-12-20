using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantumDice.Core.Entities;

namespace QuantumDice.Infrastructure.Data.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("players");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Username).HasMaxLength(100);
        builder.Property(e => e.FirstName).HasMaxLength(100);
        builder.Property(e => e.Balance).HasPrecision(18, 2);
        builder.Property(e => e.TotalDeposit).HasPrecision(18, 2);
        builder.Property(e => e.TotalWithdraw).HasPrecision(18, 2);
        builder.Property(e => e.TotalBet).HasPrecision(18, 2);
        builder.Property(e => e.TotalWin).HasPrecision(18, 2);
        
        builder.HasIndex(e => new { e.TelegramUserId, e.GroupId }).IsUnique();
        
        builder.HasOne(e => e.Group)
            .WithMany(g => g.Players)
            .HasForeignKey(e => e.GroupId);
    }
}

public class DepositConfiguration : IEntityTypeConfiguration<Deposit>
{
    public void Configure(EntityTypeBuilder<Deposit> builder)
    {
        builder.ToTable("deposits");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.BalanceBefore).HasPrecision(18, 2);
        builder.Property(e => e.BalanceAfter).HasPrecision(18, 2);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Remark).HasMaxLength(500);
        
        builder.HasOne(e => e.Player)
            .WithMany(p => p.Deposits)
            .HasForeignKey(e => e.PlayerId);
            
        builder.HasOne(e => e.Operator)
            .WithMany(d => d.ProcessedDeposits)
            .HasForeignKey(e => e.OperatorId);
    }
}

public class WithdrawalConfiguration : IEntityTypeConfiguration<Withdrawal>
{
    public void Configure(EntityTypeBuilder<Withdrawal> builder)
    {
        builder.ToTable("withdrawals");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.BalanceBefore).HasPrecision(18, 2);
        builder.Property(e => e.BalanceAfter).HasPrecision(18, 2);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Remark).HasMaxLength(500);
        
        builder.HasOne(e => e.Player)
            .WithMany(p => p.Withdrawals)
            .HasForeignKey(e => e.PlayerId);
            
        builder.HasOne(e => e.Operator)
            .WithMany(d => d.ProcessedWithdrawals)
            .HasForeignKey(e => e.OperatorId);
    }
}
