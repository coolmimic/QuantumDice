using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuantumDice.Core.Entities;
using QuantumDice.Core.Enums;
using QuantumDice.Infrastructure.Data;

namespace QuantumDice.Api.Services;

/// <summary>
/// 投注解析结果
/// </summary>
public record BetParseResult(
    bool Success,
    string? Error,
    int? BetMethodId,
    string? BetMethodCode,
    decimal Amount,
    string? ParsedContent
);

public interface IBetParseService
{
    Task<BetParseResult> ParseBetAsync(string input, long groupId, int gameTypeId);
    Task<bool> ProcessBetAsync(long playerId, long roundId, BetParseResult parseResult);
}

public class BetParseService : IBetParseService
{
    private readonly QuantumDiceDbContext _db;

    public BetParseService(QuantumDiceDbContext db)
    {
        _db = db;
    }

    public async Task<BetParseResult> ParseBetAsync(string input, long groupId, int gameTypeId)
    {
        input = input.Trim();
        
        // 获取游戏类型
        var gameType = await _db.GameTypes.FindAsync(gameTypeId);
        if (gameType == null)
            return new BetParseResult(false, "游戏类型不存在", null, null, 0, null);

        // 根据游戏类型解析
        return gameType.Code switch
        {
            "MineSweeper" => await ParseMineSweeperBetAsync(input, groupId, gameTypeId),
            "DragonTiger" => await ParseDragonTigerBetAsync(input, groupId, gameTypeId),
            "K3" => await ParseK3BetAsync(input, groupId, gameTypeId),
            _ => new BetParseResult(false, "不支持的游戏类型", null, null, 0, null)
        };
    }

    private async Task<BetParseResult> ParseMineSweeperBetAsync(string input, long groupId, int gameTypeId)
    {
        // 定位胆: 1/10, 2/10 等
        var positionMatch = Regex.Match(input, @"^([1-6])\/(\d+)$");
        if (positionMatch.Success)
        {
            var number = int.Parse(positionMatch.Groups[1].Value);
            var amount = decimal.Parse(positionMatch.Groups[2].Value);
            var method = await GetBetMethodAsync(gameTypeId, "Position");
            if (method == null) return new BetParseResult(false, "玩法未配置", null, null, 0, null);
            
            return new BetParseResult(true, null, method.Id, method.Code, amount, 
                $"{{\"numbers\":[{number}]}}");
        }

        // 大小单双: 大10, 小10, 单10, 双10
        var simpleMatch = Regex.Match(input, @"^(大|小|单|双|大单|大双|小单|小双)(\d+)$");
        if (simpleMatch.Success)
        {
            var betType = simpleMatch.Groups[1].Value;
            var amount = decimal.Parse(simpleMatch.Groups[2].Value);
            var code = betType switch
            {
                "大" => "Big",
                "小" => "Small",
                "单" => "Odd",
                "双" => "Even",
                "大单" => "BigOdd",
                "大双" => "BigEven",
                "小单" => "SmallOdd",
                "小双" => "SmallEven",
                _ => null
            };
            
            if (code == null) return new BetParseResult(false, "无效的玩法", null, null, 0, null);
            
            var method = await GetBetMethodAsync(gameTypeId, code);
            if (method == null) return new BetParseResult(false, "玩法未配置", null, null, 0, null);
            
            return new BetParseResult(true, null, method.Id, method.Code, amount, null);
        }

        return new BetParseResult(false, "无法解析投注内容", null, null, 0, null);
    }

    private async Task<BetParseResult> ParseDragonTigerBetAsync(string input, long groupId, int gameTypeId)
    {
        // 定位胆: 1/2/10 表示第1位是1, 第2位是2, 金额10
        var positionMatch = Regex.Match(input, @"^([1-6])\/([1-6])\/(\d+)$");
        if (positionMatch.Success)
        {
            var num1 = int.Parse(positionMatch.Groups[1].Value);
            var num2 = int.Parse(positionMatch.Groups[2].Value);
            var amount = decimal.Parse(positionMatch.Groups[3].Value);
            var method = await GetBetMethodAsync(gameTypeId, "Position");
            if (method == null) return new BetParseResult(false, "玩法未配置", null, null, 0, null);
            
            return new BetParseResult(true, null, method.Id, method.Code, amount,
                $"{{\"num1\":{num1},\"num2\":{num2}}}");
        }

        // 龙虎和: 龙10, 虎10, 和10
        var simpleMatch = Regex.Match(input, @"^(龙|虎|和)(\d+)$");
        if (simpleMatch.Success)
        {
            var betType = simpleMatch.Groups[1].Value;
            var amount = decimal.Parse(simpleMatch.Groups[2].Value);
            var code = betType switch
            {
                "龙" => "Dragon",
                "虎" => "Tiger",
                "和" => "Tie",
                _ => null
            };

            if (code == null) return new BetParseResult(false, "无效的玩法", null, null, 0, null);

            var method = await GetBetMethodAsync(gameTypeId, code);
            if (method == null) return new BetParseResult(false, "玩法未配置", null, null, 0, null);

            return new BetParseResult(true, null, method.Id, method.Code, amount, null);
        }

        return new BetParseResult(false, "无法解析投注内容", null, null, 0, null);
    }

    private async Task<BetParseResult> ParseK3BetAsync(string input, long groupId, int gameTypeId)
    {
        // 定位胆: 123/10 表示三个位置分别是1,2,3
        var positionMatch = Regex.Match(input, @"^([1-6])([1-6])([1-6])\/(\d+)$");
        if (positionMatch.Success)
        {
            var num1 = int.Parse(positionMatch.Groups[1].Value);
            var num2 = int.Parse(positionMatch.Groups[2].Value);
            var num3 = int.Parse(positionMatch.Groups[3].Value);
            var amount = decimal.Parse(positionMatch.Groups[4].Value);
            var method = await GetBetMethodAsync(gameTypeId, "Compound");
            if (method == null) return new BetParseResult(false, "玩法未配置", null, null, 0, null);

            return new BetParseResult(true, null, method.Id, method.Code, amount,
                $"{{\"numbers\":[{num1},{num2},{num3}]}}");
        }

        // 单星定位: 1/10 (第1位), 2/10 (第2位) 等
        var singleMatch = Regex.Match(input, @"^([1-6])\/(\d+)$");
        if (singleMatch.Success)
        {
            var number = int.Parse(singleMatch.Groups[1].Value);
            var amount = decimal.Parse(singleMatch.Groups[2].Value);
            var method = await GetBetMethodAsync(gameTypeId, "Position");
            if (method == null) return new BetParseResult(false, "玩法未配置", null, null, 0, null);

            return new BetParseResult(true, null, method.Id, method.Code, amount,
                $"{{\"numbers\":[{number}]}}");
        }

        // 大小单双龙虎: 大10, 小10, 龙10, 虎10 等
        var simplePatterns = new Dictionary<string, string>
        {
            { "大", "Big" }, { "小", "Small" },
            { "单", "Odd" }, { "双", "Even" },
            { "龙", "Dragon" }, { "虎", "Tiger" }, { "和", "Tie" },
            { "前二龙", "FrontDragon" }, { "后二龙", "BackDragon" },
            { "豹子", "Leopard" }, { "顺子", "Straight" },
            { "组三", "GroupThree" }, { "组六", "GroupSix" }
        };

        foreach (var pattern in simplePatterns)
        {
            var match = Regex.Match(input, $@"^{Regex.Escape(pattern.Key)}(\d+)$");
            if (match.Success)
            {
                var amount = decimal.Parse(match.Groups[1].Value);
                var method = await GetBetMethodAsync(gameTypeId, pattern.Value);
                if (method == null) return new BetParseResult(false, $"玩法 {pattern.Key} 未配置", null, null, 0, null);

                return new BetParseResult(true, null, method.Id, method.Code, amount, null);
            }
        }

        // 指定豹子: 111/10, 222/10 等
        var leopardMatch = Regex.Match(input, @"^([1-6])\1\1\/(\d+)$");
        if (leopardMatch.Success)
        {
            var number = int.Parse(leopardMatch.Groups[1].Value);
            var amount = decimal.Parse(leopardMatch.Groups[2].Value);
            var method = await GetBetMethodAsync(gameTypeId, "Leopard");
            if (method == null) return new BetParseResult(false, "玩法未配置", null, null, 0, null);

            return new BetParseResult(true, null, method.Id, method.Code, amount,
                $"{{\"leopard\":{number}}}");
        }

        return new BetParseResult(false, "无法解析投注内容", null, null, 0, null);
    }

    private async Task<BetMethod?> GetBetMethodAsync(int gameTypeId, string code)
    {
        return await _db.BetMethods
            .Include(m => m.BaseGame)
            .Where(m => m.BaseGame.GameTypeId == gameTypeId && m.Code == code && m.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ProcessBetAsync(long playerId, long roundId, BetParseResult parseResult)
    {
        if (!parseResult.Success || parseResult.BetMethodId == null)
            return false;

        var player = await _db.Players.FindAsync(playerId);
        if (player == null || player.IsBanned)
            return false;

        if (player.Balance < parseResult.Amount)
            return false;

        // 获取赔率配置
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Players.Any(p => p.Id == playerId));
        if (group == null) return false;

        var oddsConfig = await _db.GroupGameConfigs
            .FirstOrDefaultAsync(c => c.GroupId == group.Id && c.BetMethodId == parseResult.BetMethodId);

        var method = await _db.BetMethods.FindAsync(parseResult.BetMethodId);
        if (method == null) return false;

        var odds = oddsConfig?.CustomOdds ?? method.DefaultOdds;
        var minBet = oddsConfig?.MinBet ?? 1;
        var maxBet = oddsConfig?.MaxBet ?? 10000;

        // 检查投注限额
        if (parseResult.Amount < minBet || parseResult.Amount > maxBet)
            return false;

        // 扣除余额
        var balanceBefore = player.Balance;
        player.Balance -= parseResult.Amount;
        player.TotalBet += parseResult.Amount;

        // 创建投注记录
        var bet = new Bet
        {
            PlayerId = playerId,
            RoundId = roundId,
            BetMethodId = parseResult.BetMethodId.Value,
            BetContent = $"{parseResult.BetMethodCode}{parseResult.Amount}",
            ParsedContent = parseResult.ParsedContent,
            Amount = parseResult.Amount,
            Odds = odds,
            Status = BetStatus.Pending
        };

        _db.Bets.Add(bet);

        // 记录交易流水
        var transaction = new Transaction
        {
            PlayerId = playerId,
            Type = TransactionType.Bet,
            Amount = -parseResult.Amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = player.Balance,
            RefType = "Bets",
            RefId = bet.Id
        };
        _db.Transactions.Add(transaction);

        await _db.SaveChangesAsync();
        return true;
    }
}
