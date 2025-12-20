namespace QuantumDice.Core.Entities;

/// <summary>
/// 超级管理员
/// </summary>
public class SuperAdmin
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual ICollection<Dealer> CreatedDealers { get; set; } = new List<Dealer>();
    public virtual ICollection<Subscription> CreatedSubscriptions { get; set; } = new List<Subscription>();
}

/// <summary>
/// 系统配置
/// </summary>
public class SystemConfig
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
