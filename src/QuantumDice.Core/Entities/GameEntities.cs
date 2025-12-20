namespace QuantumDice.Core.Entities;

/// <summary>
/// 游戏类型 (第一层: 扫雷/龙虎/快三)
/// </summary>
public class GameType
{
    public int Id { get; set; }
    
    /// <summary>游戏代码 (MineSweeper, DragonTiger, K3)</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>游戏名称 (扫雷, 龙虎, 快三)</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>骰子数量 (1, 2, 3)</summary>
    public int DiceCount { get; set; }
    
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual ICollection<BaseGame> BaseGames { get; set; } = new List<BaseGame>();
    public virtual ICollection<GroupScheduleConfig> ScheduleConfigs { get; set; } = new List<GroupScheduleConfig>();
    public virtual ICollection<GameRound> Rounds { get; set; } = new List<GameRound>();
}

/// <summary>
/// 基础游戏 (第二层: 一星/二星/三星)
/// </summary>
public class BaseGame
{
    public int Id { get; set; }
    public int GameTypeId { get; set; }
    
    /// <summary>基础游戏代码 (OneStar, TwoStar, ThreeStar)</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>基础游戏名称 (一星, 二星, 三星)</summary>
    public string Name { get; set; } = string.Empty;
    
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual GameType GameType { get; set; } = null!;
    public virtual ICollection<BetMethod> BetMethods { get; set; } = new List<BetMethod>();
}

/// <summary>
/// 具体玩法 (第三层: 定位胆/大小/龙虎/豹子等)
/// </summary>
public class BetMethod
{
    public int Id { get; set; }
    public int BaseGameId { get; set; }
    
    /// <summary>玩法代码 (PositionBet, Big, Small, Dragon, Tiger, Leopard...)</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>玩法名称 (定位胆, 大, 小, 龙, 虎, 豹子...)</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>下注格式正则表达式</summary>
    public string? BetPattern { get; set; }
    
    /// <summary>默认赔率</summary>
    public decimal DefaultOdds { get; set; }
    
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual BaseGame BaseGame { get; set; } = null!;
    public virtual ICollection<GroupGameConfig> GroupConfigs { get; set; } = new List<GroupGameConfig>();
    public virtual ICollection<Bet> Bets { get; set; } = new List<Bet>();
}
