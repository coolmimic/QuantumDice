using QuantumDice.Core.Enums;

namespace QuantumDice.Core.Entities;

/// <summary>
/// 庄家 (租户)
/// </summary>
public class Dealer
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>联系方式 (Telegram用户名)</summary>
    public string? ContactTelegram { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>创建者 (超级管理员ID)</summary>
    public int? CreatedBy { get; set; }
    
    // Navigation
    public virtual SuperAdmin? Creator { get; set; }
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    public virtual ICollection<Deposit> ProcessedDeposits { get; set; } = new List<Deposit>();
    public virtual ICollection<Withdrawal> ProcessedWithdrawals { get; set; } = new List<Withdrawal>();
}

/// <summary>
/// 订阅记录
/// </summary>
public class Subscription
{
    public int Id { get; set; }
    public int DealerId { get; set; }
    
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    /// <summary>订阅费用</summary>
    public decimal? Amount { get; set; }
    
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>创建者 (超级管理员ID)</summary>
    public int? CreatedBy { get; set; }
    
    // Navigation
    public virtual Dealer Dealer { get; set; } = null!;
    public virtual SuperAdmin? Creator { get; set; }
}
