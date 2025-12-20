using QuantumDice.Core.Enums;

namespace QuantumDice.Core.Entities;

/// <summary>
/// 玩家 (群组级隔离)
/// </summary>
public class Player
{
    public long Id { get; set; }
    
    /// <summary>Telegram用户ID</summary>
    public long TelegramUserId { get; set; }
    
    public long GroupId { get; set; }
    
    /// <summary>Telegram用户名</summary>
    public string? Username { get; set; }
    
    /// <summary>名字</summary>
    public string? FirstName { get; set; }
    
    /// <summary>当前余额</summary>
    public decimal Balance { get; set; }
    
    /// <summary>累计充值</summary>
    public decimal TotalDeposit { get; set; }
    
    /// <summary>累计提现</summary>
    public decimal TotalWithdraw { get; set; }
    
    /// <summary>累计投注</summary>
    public decimal TotalBet { get; set; }
    
    /// <summary>累计中奖</summary>
    public decimal TotalWin { get; set; }
    
    /// <summary>是否封禁</summary>
    public bool IsBanned { get; set; }
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveAt { get; set; }
    
    // Navigation
    public virtual Group Group { get; set; } = null!;
    public virtual ICollection<Bet> Bets { get; set; } = new List<Bet>();
    public virtual ICollection<Deposit> Deposits { get; set; } = new List<Deposit>();
    public virtual ICollection<Withdrawal> Withdrawals { get; set; } = new List<Withdrawal>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

/// <summary>
/// 充值记录
/// </summary>
public class Deposit
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    
    public DepositStatus Status { get; set; } = DepositStatus.Completed;
    
    /// <summary>操作人 (庄家ID)</summary>
    public int? OperatorId { get; set; }
    
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual Player Player { get; set; } = null!;
    public virtual Dealer? Operator { get; set; }
}

/// <summary>
/// 提现记录
/// </summary>
public class Withdrawal
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    
    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;
    
    /// <summary>操作人 (庄家ID)</summary>
    public int? OperatorId { get; set; }
    
    public string? Remark { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation
    public virtual Player Player { get; set; } = null!;
    public virtual Dealer? Operator { get; set; }
}
