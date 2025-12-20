using Microsoft.EntityFrameworkCore;
using QuantumDice.Api.DTOs;
using QuantumDice.Core.Entities;
using QuantumDice.Infrastructure.Data;

namespace QuantumDice.Api.Services;

public interface IGroupService
{
    Task<List<GroupDto>> GetGroupsByDealerAsync(int dealerId);
    Task<GroupDto?> GetGroupAsync(long groupId);
    Task<Group> BindGroupAsync(int dealerId, BindGroupRequest request);
    Task<bool> UnbindGroupAsync(long groupId);
    Task<List<OddsConfigDto>> GetOddsConfigAsync(long groupId);
    Task<bool> UpdateOddsConfigAsync(long groupId, UpdateOddsRequest request);
}

public class GroupService : IGroupService
{
    private readonly QuantumDiceDbContext _db;

    public GroupService(QuantumDiceDbContext db)
    {
        _db = db;
    }

    public async Task<List<GroupDto>> GetGroupsByDealerAsync(int dealerId)
    {
        return await _db.Groups
            .Where(g => g.DealerId == dealerId)
            .Select(g => new GroupDto(
                g.Id,
                g.TelegramGroupId,
                g.GroupName,
                g.IsActive,
                g.BoundAt,
                g.Players.Count
            ))
            .ToListAsync();
    }

    public async Task<GroupDto?> GetGroupAsync(long groupId)
    {
        var g = await _db.Groups
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (g == null) return null;

        return new GroupDto(
            g.Id,
            g.TelegramGroupId,
            g.GroupName,
            g.IsActive,
            g.BoundAt,
            g.Players.Count
        );
    }

    public async Task<Group> BindGroupAsync(int dealerId, BindGroupRequest request)
    {
        // 检查是否已绑定
        var existing = await _db.Groups
            .FirstOrDefaultAsync(g => g.TelegramGroupId == request.TelegramGroupId);

        if (existing != null)
        {
            // 更新绑定
            existing.DealerId = dealerId;
            existing.GroupName = request.GroupName;
            existing.IsActive = true;
            await _db.SaveChangesAsync();
            return existing;
        }

        var group = new Group
        {
            TelegramGroupId = request.TelegramGroupId,
            DealerId = dealerId,
            GroupName = request.GroupName,
            IsActive = true
        };

        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        // 初始化默认赔率配置
        await InitializeDefaultOddsAsync(group.Id);

        return group;
    }

    private async Task InitializeDefaultOddsAsync(long groupId)
    {
        var betMethods = await _db.BetMethods.ToListAsync();
        
        foreach (var method in betMethods)
        {
            var config = new GroupGameConfig
            {
                GroupId = groupId,
                BetMethodId = method.Id,
                IsEnabled = true,
                MinBet = 1,
                MaxBet = 10000
            };
            _db.GroupGameConfigs.Add(config);
        }

        // 初始化游戏频率配置
        var gameTypes = await _db.GameTypes.ToListAsync();
        foreach (var gameType in gameTypes)
        {
            var scheduleConfig = new GroupScheduleConfig
            {
                GroupId = groupId,
                GameTypeId = gameType.Id,
                IntervalMinutes = 5,
                IsEnabled = true
            };
            _db.GroupScheduleConfigs.Add(scheduleConfig);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<bool> UnbindGroupAsync(long groupId)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return false;

        group.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<OddsConfigDto>> GetOddsConfigAsync(long groupId)
    {
        return await _db.GroupGameConfigs
            .Where(c => c.GroupId == groupId)
            .Include(c => c.BetMethod)
                .ThenInclude(b => b.BaseGame)
                    .ThenInclude(bg => bg.GameType)
            .Select(c => new OddsConfigDto(
                c.BetMethodId,
                c.BetMethod.Name,
                c.BetMethod.BaseGame.Name,
                c.BetMethod.BaseGame.GameType.Name,
                c.BetMethod.DefaultOdds,
                c.CustomOdds,
                c.MinBet,
                c.MaxBet,
                c.IsEnabled
            ))
            .ToListAsync();
    }

    public async Task<bool> UpdateOddsConfigAsync(long groupId, UpdateOddsRequest request)
    {
        var config = await _db.GroupGameConfigs
            .FirstOrDefaultAsync(c => c.GroupId == groupId && c.BetMethodId == request.BetMethodId);

        if (config == null) return false;

        config.CustomOdds = request.CustomOdds;
        config.MinBet = request.MinBet;
        config.MaxBet = request.MaxBet;
        config.IsEnabled = request.IsEnabled;

        await _db.SaveChangesAsync();
        return true;
    }
}
