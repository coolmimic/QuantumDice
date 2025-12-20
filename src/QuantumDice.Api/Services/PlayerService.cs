using Microsoft.EntityFrameworkCore;
using QuantumDice.Api.DTOs;
using QuantumDice.Core.Entities;
using QuantumDice.Core.Enums;
using QuantumDice.Infrastructure.Data;

namespace QuantumDice.Api.Services;

public interface IPlayerService
{
    Task<List<PlayerDto>> GetPlayersByGroupAsync(long groupId);
    Task<PlayerDto?> GetPlayerAsync(long playerId);
    Task<Player> GetOrCreatePlayerAsync(long telegramUserId, long groupId, string? username, string? firstName);
    Task<bool> AdjustBalanceAsync(long playerId, decimal amount, string remark, int operatorId, bool isDeposit);
    Task<bool> BanPlayerAsync(long playerId, bool isBanned);
}

public class PlayerService : IPlayerService
{
    private readonly QuantumDiceDbContext _db;

    public PlayerService(QuantumDiceDbContext db)
    {
        _db = db;
    }

    public async Task<List<PlayerDto>> GetPlayersByGroupAsync(long groupId)
    {
        return await _db.Players
            .Where(p => p.GroupId == groupId)
            .OrderByDescending(p => p.LastActiveAt)
            .Select(p => new PlayerDto(
                p.Id,
                p.TelegramUserId,
                p.Username,
                p.FirstName,
                p.Balance,
                p.TotalDeposit,
                p.TotalWithdraw,
                p.TotalBet,
                p.TotalWin,
                p.IsBanned,
                p.JoinedAt,
                p.LastActiveAt
            ))
            .ToListAsync();
    }

    public async Task<PlayerDto?> GetPlayerAsync(long playerId)
    {
        var p = await _db.Players.FindAsync(playerId);
        if (p == null) return null;

        return new PlayerDto(
            p.Id,
            p.TelegramUserId,
            p.Username,
            p.FirstName,
            p.Balance,
            p.TotalDeposit,
            p.TotalWithdraw,
            p.TotalBet,
            p.TotalWin,
            p.IsBanned,
            p.JoinedAt,
            p.LastActiveAt
        );
    }

    public async Task<Player> GetOrCreatePlayerAsync(long telegramUserId, long groupId, string? username, string? firstName)
    {
        var player = await _db.Players
            .FirstOrDefaultAsync(p => p.TelegramUserId == telegramUserId && p.GroupId == groupId);

        if (player == null)
        {
            player = new Player
            {
                TelegramUserId = telegramUserId,
                GroupId = groupId,
                Username = username,
                FirstName = firstName
            };
            _db.Players.Add(player);
            await _db.SaveChangesAsync();
        }
        else
        {
            // 更新用户名
            player.Username = username;
            player.FirstName = firstName;
            player.LastActiveAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return player;
    }

    public async Task<bool> AdjustBalanceAsync(long playerId, decimal amount, string remark, int operatorId, bool isDeposit)
    {
        var player = await _db.Players.FindAsync(playerId);
        if (player == null) return false;

        var balanceBefore = player.Balance;
        player.Balance += amount;
        var balanceAfter = player.Balance;

        if (isDeposit)
        {
            player.TotalDeposit += Math.Abs(amount);
            var deposit = new Deposit
            {
                PlayerId = playerId,
                Amount = Math.Abs(amount),
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                Status = DepositStatus.Completed,
                OperatorId = operatorId,
                Remark = remark
            };
            _db.Deposits.Add(deposit);
        }
        else
        {
            player.TotalWithdraw += Math.Abs(amount);
            var withdrawal = new Withdrawal
            {
                PlayerId = playerId,
                Amount = Math.Abs(amount),
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                Status = WithdrawalStatus.Approved,
                OperatorId = operatorId,
                Remark = remark,
                ProcessedAt = DateTime.UtcNow
            };
            _db.Withdrawals.Add(withdrawal);
        }

        // 记录交易流水
        var transaction = new Transaction
        {
            PlayerId = playerId,
            Type = isDeposit ? TransactionType.Deposit : TransactionType.Withdraw,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            Remark = remark
        };
        _db.Transactions.Add(transaction);

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> BanPlayerAsync(long playerId, bool isBanned)
    {
        var player = await _db.Players.FindAsync(playerId);
        if (player == null) return false;

        player.IsBanned = isBanned;
        await _db.SaveChangesAsync();
        return true;
    }
}
