using QuantumDice.Core.Enums;

namespace QuantumDice.Core.Entities;

/// <summary>
/// 交易流水 (所有资金变动)
/// </summary>
public class Transaction
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    
    public TransactionType Type { get; set; }
    
    /// <summary>变动金额 (正数增加，负数减少)</summary>
    public decimal Amount { get; set; }
    
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    
    /// <summary>关联类型 (Deposits, Withdrawals, Bets)</summary>
    public string? RefType { get; set; }
    
    /// <summary>关联ID</summary>
    public long? RefId { get; set; }
    
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual Player Player { get; set; } = null!;
}

/// <summary>
/// 操作日志
/// </summary>
public class OperationLog
{
    public long Id { get; set; }
    
    public OperatorType OperatorType { get; set; }
    public int OperatorId { get; set; }
    
    /// <summary>操作动作</summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>目标类型</summary>
    public string? TargetType { get; set; }
    
    /// <summary>目标ID</summary>
    public long? TargetId { get; set; }
    
    /// <summary>详情 (JSON)</summary>
    public string? Detail { get; set; }
    
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
