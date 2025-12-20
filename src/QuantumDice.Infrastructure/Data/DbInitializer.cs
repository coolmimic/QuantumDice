using Microsoft.EntityFrameworkCore;
using QuantumDice.Core.Entities;
using QuantumDice.Core.Enums;

namespace QuantumDice.Infrastructure.Data;

/// <summary>
/// 数据库种子数据初始化
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(QuantumDiceDbContext context)
    {
        // 确保数据库已创建
        await context.Database.EnsureCreatedAsync();
        
        // 如果已有数据则跳过
        if (await context.GameTypes.AnyAsync())
            return;
        
        // 初始化游戏类型
        await SeedGameTypesAsync(context);
        
        // 初始化系统配置
        await SeedSystemConfigsAsync(context);
        
        // 初始化超级管理员
        await SeedSuperAdminAsync(context);
        
        await context.SaveChangesAsync();
    }
    
    private static async Task SeedGameTypesAsync(QuantumDiceDbContext context)
    {
        // ========== 扫雷 (1骰子) ==========
        var mineSweeper = new GameType
        {
            Code = "MineSweeper",
            Name = "扫雷",
            DiceCount = 1,
            SortOrder = 1,
            IsActive = true
        };
        context.GameTypes.Add(mineSweeper);
        await context.SaveChangesAsync();
        
        var mineOneStar = new BaseGame
        {
            GameTypeId = mineSweeper.Id,
            Code = "OneStar",
            Name = "一星",
            SortOrder = 1,
            IsActive = true
        };
        context.BaseGames.Add(mineOneStar);
        await context.SaveChangesAsync();
        
        // 扫雷玩法
        var mineBetMethods = new List<BetMethod>
        {
            new() { BaseGameId = mineOneStar.Id, Code = "Position", Name = "定位胆", DefaultOdds = 5.8m, BetPattern = @"^\d$", SortOrder = 1 },
            new() { BaseGameId = mineOneStar.Id, Code = "Big", Name = "大", DefaultOdds = 1.96m, BetPattern = @"^大\d+$", SortOrder = 2 },
            new() { BaseGameId = mineOneStar.Id, Code = "Small", Name = "小", DefaultOdds = 1.96m, BetPattern = @"^小\d+$", SortOrder = 3 },
            new() { BaseGameId = mineOneStar.Id, Code = "Odd", Name = "单", DefaultOdds = 1.96m, BetPattern = @"^单\d+$", SortOrder = 4 },
            new() { BaseGameId = mineOneStar.Id, Code = "Even", Name = "双", DefaultOdds = 1.96m, BetPattern = @"^双\d+$", SortOrder = 5 },
            new() { BaseGameId = mineOneStar.Id, Code = "BigOdd", Name = "大单", DefaultOdds = 3.8m, SortOrder = 6 },
            new() { BaseGameId = mineOneStar.Id, Code = "BigEven", Name = "大双", DefaultOdds = 3.8m, SortOrder = 7 },
            new() { BaseGameId = mineOneStar.Id, Code = "SmallOdd", Name = "小单", DefaultOdds = 3.8m, SortOrder = 8 },
            new() { BaseGameId = mineOneStar.Id, Code = "SmallEven", Name = "小双", DefaultOdds = 3.8m, SortOrder = 9 }
        };
        context.BetMethods.AddRange(mineBetMethods);
        
        // ========== 龙虎 (2骰子) ==========
        var dragonTiger = new GameType
        {
            Code = "DragonTiger",
            Name = "龙虎",
            DiceCount = 2,
            SortOrder = 2,
            IsActive = true
        };
        context.GameTypes.Add(dragonTiger);
        await context.SaveChangesAsync();
        
        var dtOneStar = new BaseGame { GameTypeId = dragonTiger.Id, Code = "OneStar", Name = "一星", SortOrder = 1, IsActive = true };
        var dtTwoStar = new BaseGame { GameTypeId = dragonTiger.Id, Code = "TwoStar", Name = "二星", SortOrder = 2, IsActive = true };
        context.BaseGames.AddRange(dtOneStar, dtTwoStar);
        await context.SaveChangesAsync();
        
        var dtBetMethods = new List<BetMethod>
        {
            // 一星玩法
            new() { BaseGameId = dtOneStar.Id, Code = "Position", Name = "定位胆", DefaultOdds = 5.8m, SortOrder = 1 },
            // 二星玩法
            new() { BaseGameId = dtTwoStar.Id, Code = "Dragon", Name = "龙", DefaultOdds = 1.96m, Description = "第1骰 > 第2骰", SortOrder = 1 },
            new() { BaseGameId = dtTwoStar.Id, Code = "Tiger", Name = "虎", DefaultOdds = 1.96m, Description = "第1骰 < 第2骰", SortOrder = 2 },
            new() { BaseGameId = dtTwoStar.Id, Code = "Tie", Name = "和", DefaultOdds = 8.0m, Description = "第1骰 = 第2骰", SortOrder = 3 }
        };
        context.BetMethods.AddRange(dtBetMethods);
        
        // ========== 快三 (3骰子) ==========
        var k3 = new GameType
        {
            Code = "K3",
            Name = "快三",
            DiceCount = 3,
            SortOrder = 3,
            IsActive = true
        };
        context.GameTypes.Add(k3);
        await context.SaveChangesAsync();
        
        var k3OneStar = new BaseGame { GameTypeId = k3.Id, Code = "OneStar", Name = "一星", SortOrder = 1, IsActive = true };
        var k3TwoStar = new BaseGame { GameTypeId = k3.Id, Code = "TwoStar", Name = "二星", SortOrder = 2, IsActive = true };
        var k3ThreeStar = new BaseGame { GameTypeId = k3.Id, Code = "ThreeStar", Name = "三星", SortOrder = 3, IsActive = true };
        context.BaseGames.AddRange(k3OneStar, k3TwoStar, k3ThreeStar);
        await context.SaveChangesAsync();
        
        var k3BetMethods = new List<BetMethod>
        {
            // 一星
            new() { BaseGameId = k3OneStar.Id, Code = "Position", Name = "定位胆", DefaultOdds = 5.8m, SortOrder = 1 },
            new() { BaseGameId = k3OneStar.Id, Code = "Big", Name = "大", DefaultOdds = 1.96m, Description = "总和11-17，豹子通杀", SortOrder = 2 },
            new() { BaseGameId = k3OneStar.Id, Code = "Small", Name = "小", DefaultOdds = 1.96m, Description = "总和4-10，豹子通杀", SortOrder = 3 },
            new() { BaseGameId = k3OneStar.Id, Code = "Odd", Name = "单", DefaultOdds = 1.96m, Description = "总和为单数，豹子通杀", SortOrder = 4 },
            new() { BaseGameId = k3OneStar.Id, Code = "Even", Name = "双", DefaultOdds = 1.96m, Description = "总和为双数，豹子通杀", SortOrder = 5 },
            
            // 二星
            new() { BaseGameId = k3TwoStar.Id, Code = "Dragon", Name = "龙", DefaultOdds = 1.96m, Description = "第1骰 > 第3骰", SortOrder = 1 },
            new() { BaseGameId = k3TwoStar.Id, Code = "Tiger", Name = "虎", DefaultOdds = 1.96m, Description = "第1骰 < 第3骰", SortOrder = 2 },
            new() { BaseGameId = k3TwoStar.Id, Code = "Tie", Name = "和", DefaultOdds = 8.0m, Description = "第1骰 = 第3骰", SortOrder = 3 },
            new() { BaseGameId = k3TwoStar.Id, Code = "FrontDragon", Name = "前二龙", DefaultOdds = 1.96m, Description = "第1骰 > 第2骰", SortOrder = 4 },
            new() { BaseGameId = k3TwoStar.Id, Code = "BackDragon", Name = "后二龙", DefaultOdds = 1.96m, Description = "第2骰 > 第3骰", SortOrder = 5 },
            
            // 三星
            new() { BaseGameId = k3ThreeStar.Id, Code = "Compound", Name = "复式", DefaultOdds = 5.8m, Description = "如123/1", SortOrder = 1 },
            new() { BaseGameId = k3ThreeStar.Id, Code = "Leopard", Name = "豹子", DefaultOdds = 30.0m, Description = "三骰相同", SortOrder = 2 },
            new() { BaseGameId = k3ThreeStar.Id, Code = "Straight", Name = "顺子", DefaultOdds = 8.0m, Description = "如123,234,支持循环", SortOrder = 3 },
            new() { BaseGameId = k3ThreeStar.Id, Code = "GroupThree", Name = "组三", DefaultOdds = 2.5m, Description = "一对", SortOrder = 4 },
            new() { BaseGameId = k3ThreeStar.Id, Code = "GroupSix", Name = "组六", DefaultOdds = 1.5m, Description = "全不同", SortOrder = 5 }
        };
        context.BetMethods.AddRange(k3BetMethods);
        
        await context.SaveChangesAsync();
    }
    
    private static async Task SeedSystemConfigsAsync(QuantumDiceDbContext context)
    {
        var configs = new List<SystemConfig>
        {
            new() { Key = "DefaultMinBet", Value = "1", Description = "默认最小投注额" },
            new() { Key = "DefaultMaxBet", Value = "10000", Description = "默认最大投注额" },
            new() { Key = "DefaultRoundInterval", Value = "5", Description = "默认游戏轮次间隔(分钟)" },
            new() { Key = "BetCloseSeconds", Value = "30", Description = "封盘前倒计时(秒)" },
            new() { Key = "PlatformName", Value = "QuantumDice", Description = "平台名称" },
            new() { Key = "Version", Value = "1.0.0", Description = "系统版本" }
        };
        
        context.SystemConfigs.AddRange(configs);
        await context.SaveChangesAsync();
    }
    
    private static async Task SeedSuperAdminAsync(QuantumDiceDbContext context)
    {
        var admin = new SuperAdmin
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            IsActive = true
        };
        
        context.SuperAdmins.Add(admin);
        await context.SaveChangesAsync();
    }
}
