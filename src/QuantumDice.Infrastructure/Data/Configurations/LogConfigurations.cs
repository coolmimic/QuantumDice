using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuantumDice.Core.Entities;

namespace QuantumDice.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.BalanceBefore).HasPrecision(18, 2);
        builder.Property(e => e.BalanceAfter).HasPrecision(18, 2);
        builder.Property(e => e.RefType).HasMaxLength(20);
        builder.Property(e => e.Remark).HasMaxLength(500);
        
        builder.HasOne(e => e.Player)
            .WithMany(p => p.Transactions)
            .HasForeignKey(e => e.PlayerId);
    }
}

public class OperationLogConfiguration : IEntityTypeConfiguration<OperationLog>
{
    public void Configure(EntityTypeBuilder<OperationLog> builder)
    {
        builder.ToTable("operation_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OperatorType).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Action).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TargetType).HasMaxLength(50);
        builder.Property(e => e.Detail).HasColumnType("jsonb");
        builder.Property(e => e.IpAddress).HasMaxLength(50);
    }
}
