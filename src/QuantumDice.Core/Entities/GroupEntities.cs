namespace QuantumDice.Core.Entities;

/// <summary>
/// Telegram 群组
/// </summary>
public class Group
{
    public long Id { get; set; }
    
    /// <summary>Telegram群组ID</summary>
    public long TelegramGroupId { get; set; }
    
    public int DealerId { get; set; }
    
    /// <summary>群组名称</summary>
    public string? GroupName { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime BoundAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual Dealer Dealer { get; set; } = null!;
    public virtual ICollection<GroupGameConfig> GameConfigs { get; set; } = new List<GroupGameConfig>();
    public virtual ICollection<GroupScheduleConfig> ScheduleConfigs { get; set; } = new List<GroupScheduleConfig>();
    public virtual ICollection<Player> Players { get; set; } = new List<Player>();
    public virtual ICollection<GameRound> Rounds { get; set; } = new List<GameRound>();
}

/// <summary>
/// 群组游戏配置 (玩法级别，含自定义赔率)
/// </summary>
public class GroupGameConfig
{
    public int Id { get; set; }
    public long GroupId { get; set; }
    public int BetMethodId { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>自定义赔率 (NULL则使用默认赔率)</summary>
    public decimal? CustomOdds { get; set; }
    
    /// <summary>最小投注额</summary>
    public decimal MinBet { get; set; } = 1;
    
    /// <summary>最大投注额</summary>
    public decimal MaxBet { get; set; } = 10000;
    
    // Navigation
    public virtual Group Group { get; set; } = null!;
    public virtual BetMethod BetMethod { get; set; } = null!;
}

/// <summary>
/// 群组游戏频率配置 (游戏类型级别)
/// </summary>
public class GroupScheduleConfig
{
    public int Id { get; set; }
    public long GroupId { get; set; }
    public int GameTypeId { get; set; }
    
    /// <summary>开奖间隔 (分钟)</summary>
    public int IntervalMinutes { get; set; } = 5;
    
    public bool IsEnabled { get; set; } = true;
    
    // Navigation
    public virtual Group Group { get; set; } = null!;
    public virtual GameType GameType { get; set; } = null!;
}
