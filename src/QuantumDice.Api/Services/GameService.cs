using Microsoft.EntityFrameworkCore;
using QuantumDice.Api.DTOs;
using QuantumDice.Core.Entities;
using QuantumDice.Core.Enums;
using QuantumDice.Infrastructure.Data;

namespace QuantumDice.Api.Services;

public interface IGameService
{
    Task<List<GameTypeDto>> GetGameTypesAsync();
    Task<List<BaseGameDto>> GetBaseGamesAsync(int gameTypeId);
    Task<List<BetMethodDto>> GetBetMethodsAsync(int baseGameId);
    Task<GameRound?> GetCurrentRoundAsync(long groupId, int gameTypeId);
    Task<GameRound> StartNewRoundAsync(long groupId, int gameTypeId, int intervalMinutes);
    Task<List<int>> DrawDiceAsync(long roundId, int diceCount);
    Task SettleRoundAsync(long roundId);
}

public class GameService : IGameService
{
    private readonly QuantumDiceDbContext _db;

    public GameService(QuantumDiceDbContext db)
    {
        _db = db;
    }

    public async Task<List<GameTypeDto>> GetGameTypesAsync()
    {
        return await _db.GameTypes
            .Where(g => g.IsActive)
            .OrderBy(g => g.SortOrder)
            .Select(g => new GameTypeDto(g.Id, g.Code, g.Name, g.DiceCount, g.IsActive))
            .ToListAsync();
    }

    public async Task<List<BaseGameDto>> GetBaseGamesAsync(int gameTypeId)
    {
        return await _db.BaseGames
            .Where(b => b.GameTypeId == gameTypeId && b.IsActive)
            .OrderBy(b => b.SortOrder)
            .Select(b => new BaseGameDto(b.Id, b.Code, b.Name, b.GameTypeId))
            .ToListAsync();
    }

    public async Task<List<BetMethodDto>> GetBetMethodsAsync(int baseGameId)
    {
        return await _db.BetMethods
            .Where(m => m.BaseGameId == baseGameId && m.IsActive)
            .OrderBy(m => m.SortOrder)
            .Select(m => new BetMethodDto(m.Id, m.Code, m.Name, m.DefaultOdds, m.BaseGameId))
            .ToListAsync();
    }

    public async Task<GameRound?> GetCurrentRoundAsync(long groupId, int gameTypeId)
    {
        return await _db.GameRounds
            .Where(r => r.GroupId == groupId && r.GameTypeId == gameTypeId)
            .Where(r => r.Status == RoundStatus.Betting || r.Status == RoundStatus.Closed)
            .OrderByDescending(r => r.OpenTime)
            .FirstOrDefaultAsync();
    }

    public async Task<GameRound> StartNewRoundAsync(long groupId, int gameTypeId, int intervalMinutes)
    {
        var now = DateTime.UtcNow;
        var roundNumber = now.ToString("yyyyMMddHHmmss");

        var round = new GameRound
        {
            GroupId = groupId,
            GameTypeId = gameTypeId,
            RoundNumber = roundNumber,
            Status = RoundStatus.Betting,
            OpenTime = now,
            CloseTime = now.AddMinutes(intervalMinutes).AddSeconds(-30) // 封盘时间
        };

        _db.GameRounds.Add(round);
        await _db.SaveChangesAsync();

        return round;
    }

    public async Task<List<int>> DrawDiceAsync(long roundId, int diceCount)
    {
        var random = new Random();
        var results = new List<int>();

        for (int i = 1; i <= diceCount; i++)
        {
            var value = random.Next(1, 7);
            results.Add(value);

            var diceResult = new DiceResult
            {
                RoundId = roundId,
                DiceIndex = i,
                Value = value
            };
            _db.DiceResults.Add(diceResult);
        }

        var round = await _db.GameRounds.FindAsync(roundId);
        if (round != null)
        {
            round.Status = RoundStatus.Drawing;
            round.DrawTime = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return results;
    }

    public async Task SettleRoundAsync(long roundId)
    {
        var round = await _db.GameRounds
            .Include(r => r.DiceResults)
            .Include(r => r.Bets)
                .ThenInclude(b => b.Player)
            .Include(r => r.Bets)
                .ThenInclude(b => b.BetMethod)
            .FirstOrDefaultAsync(r => r.Id == roundId);

        if (round == null) return;

        var diceValues = round.DiceResults.OrderBy(d => d.DiceIndex).Select(d => d.Value).ToArray();

        foreach (var bet in round.Bets.Where(b => b.Status == BetStatus.Pending))
        {
            var isWin = CheckBetResult(bet.BetMethod.Code, bet.BetContent, diceValues);
            
            if (isWin)
            {
                bet.Status = BetStatus.Won;
                bet.WinAmount = bet.Amount * bet.Odds;
                
                // 更新玩家余额
                bet.Player.Balance += bet.WinAmount;
                bet.Player.TotalWin += bet.WinAmount;

                // 记录交易
                var transaction = new Transaction
                {
                    PlayerId = bet.PlayerId,
                    Type = TransactionType.Win,
                    Amount = bet.WinAmount,
                    BalanceBefore = bet.Player.Balance - bet.WinAmount,
                    BalanceAfter = bet.Player.Balance,
                    RefType = "Bets",
                    RefId = bet.Id
                };
                _db.Transactions.Add(transaction);
            }
            else
            {
                bet.Status = BetStatus.Lost;
            }
        }

        round.Status = RoundStatus.Settled;
        await _db.SaveChangesAsync();
    }

    private bool CheckBetResult(string betMethodCode, string betContent, int[] diceValues)
    {
        // 这里是简化的判断逻辑，实际需要完整实现各种玩法
        var total = diceValues.Sum();
        
        return betMethodCode switch
        {
            "Big" => total >= 11 && total <= 17 && !IsLeopard(diceValues),
            "Small" => total >= 4 && total <= 10 && !IsLeopard(diceValues),
            "Odd" => total % 2 == 1 && !IsLeopard(diceValues),
            "Even" => total % 2 == 0 && !IsLeopard(diceValues),
            "Leopard" => IsLeopard(diceValues),
            "Dragon" => diceValues.Length >= 2 && diceValues[0] > diceValues[^1],
            "Tiger" => diceValues.Length >= 2 && diceValues[0] < diceValues[^1],
            "Tie" => diceValues.Length >= 2 && diceValues[0] == diceValues[^1],
            _ => false
        };
    }

    private bool IsLeopard(int[] values) => values.Length == 3 && values.Distinct().Count() == 1;
}
