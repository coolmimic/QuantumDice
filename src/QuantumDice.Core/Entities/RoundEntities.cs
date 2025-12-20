using QuantumDice.Core.Enums;

namespace QuantumDice.Core.Entities;

/// <summary>
/// 游戏轮次
/// </summary>
public class GameRound
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public int GameTypeId { get; set; }
    
    /// <summary>期号 (如 2023122001)</summary>
    public string RoundNumber { get; set; } = string.Empty;
    
    public RoundStatus Status { get; set; } = RoundStatus.Betting;
    
    /// <summary>开盘时间</summary>
    public DateTime OpenTime { get; set; }
    
    /// <summary>封盘时间</summary>
    public DateTime CloseTime { get; set; }
    
    /// <summary>开奖时间</summary>
    public DateTime? DrawTime { get; set; }
    
    // Navigation
    public virtual Group Group { get; set; } = null!;
    public virtual GameType GameType { get; set; } = null!;
    public virtual ICollection<DiceResult> DiceResults { get; set; } = new List<DiceResult>();
    public virtual ICollection<Bet> Bets { get; set; } = new List<Bet>();
}

/// <summary>
/// 骰子结果
/// </summary>
public class DiceResult
{
    public long Id { get; set; }
    public long RoundId { get; set; }
    
    /// <summary>骰子序号 (1, 2, 3)</summary>
    public int DiceIndex { get; set; }
    
    /// <summary>点数 (1-6)</summary>
    public int Value { get; set; }
    
    /// <summary>Telegram消息ID</summary>
    public long? TelegramMsgId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual GameRound Round { get; set; } = null!;
}

/// <summary>
/// 投注记录
/// </summary>
public class Bet
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public long RoundId { get; set; }
    public int BetMethodId { get; set; }
    
    /// <summary>原始下注内容 (如 "123/1", "大10")</summary>
    public string BetContent { get; set; } = string.Empty;
    
    /// <summary>解析后的结构化内容 (JSON)</summary>
    public string? ParsedContent { get; set; }
    
    /// <summary>投注金额</summary>
    public decimal Amount { get; set; }
    
    /// <summary>使用的赔率</summary>
    public decimal Odds { get; set; }
    
    /// <summary>中奖金额</summary>
    public decimal WinAmount { get; set; }
    
    public BetStatus Status { get; set; } = BetStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual Player Player { get; set; } = null!;
    public virtual GameRound Round { get; set; } = null!;
    public virtual BetMethod BetMethod { get; set; } = null!;
}
