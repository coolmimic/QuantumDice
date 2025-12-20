using Microsoft.EntityFrameworkCore;
using QuantumDice.Api.DTOs;
using QuantumDice.Core.Entities;
using QuantumDice.Core.Enums;
using QuantumDice.Infrastructure.Data;

namespace QuantumDice.Api.Services;

public interface IDealerService
{
    Task<List<DealerDto>> GetAllDealersAsync();
    Task<DealerDto?> GetDealerByIdAsync(int id);
    Task<Dealer> CreateDealerAsync(CreateDealerRequest request);
    Task<bool> UpdateDealerAsync(int id, UpdateDealerRequest request);
    Task<bool> ExtendSubscriptionAsync(int dealerId, ExtendSubscriptionRequest request);
    Task<bool> IsSubscriptionValidAsync(int dealerId);
}

public class DealerService : IDealerService
{
    private readonly QuantumDiceDbContext _db;

    public DealerService(QuantumDiceDbContext db)
    {
        _db = db;
    }

    public async Task<List<DealerDto>> GetAllDealersAsync()
    {
        var dealers = await _db.Dealers
            .Include(d => d.Groups)
            .Include(d => d.Subscriptions)
            .ToListAsync();

        return dealers.Select(d => new DealerDto(
            d.Id,
            d.Username,
            d.ContactTelegram,
            d.IsActive,
            d.CreatedAt,
            d.Subscriptions.Where(s => s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.EndTime)
                .FirstOrDefault()?.EndTime,
            d.Groups.Count
        )).ToList();
    }

    public async Task<DealerDto?> GetDealerByIdAsync(int id)
    {
        var d = await _db.Dealers
            .Include(d => d.Groups)
            .Include(d => d.Subscriptions)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (d == null) return null;

        return new DealerDto(
            d.Id,
            d.Username,
            d.ContactTelegram,
            d.IsActive,
            d.CreatedAt,
            d.Subscriptions.Where(s => s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.EndTime)
                .FirstOrDefault()?.EndTime,
            d.Groups.Count
        );
    }

    public async Task<Dealer> CreateDealerAsync(CreateDealerRequest request)
    {
        var dealer = new Dealer
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            ContactTelegram = request.ContactTelegram,
            IsActive = true
        };

        _db.Dealers.Add(dealer);
        await _db.SaveChangesAsync();

        // 创建订阅
        var subscription = new Subscription
        {
            DealerId = dealer.Id,
            StartTime = DateTime.UtcNow,
            EndTime = request.SubscriptionEndTime,
            Status = SubscriptionStatus.Active
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        return dealer;
    }

    public async Task<bool> UpdateDealerAsync(int id, UpdateDealerRequest request)
    {
        var dealer = await _db.Dealers.FindAsync(id);
        if (dealer == null) return false;

        if (request.ContactTelegram != null)
            dealer.ContactTelegram = request.ContactTelegram;
        if (request.IsActive.HasValue)
            dealer.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExtendSubscriptionAsync(int dealerId, ExtendSubscriptionRequest request)
    {
        var dealer = await _db.Dealers.FindAsync(dealerId);
        if (dealer == null) return false;

        // 将现有活跃订阅设为过期
        var activeSubscriptions = await _db.Subscriptions
            .Where(s => s.DealerId == dealerId && s.Status == SubscriptionStatus.Active)
            .ToListAsync();

        foreach (var sub in activeSubscriptions)
        {
            sub.Status = SubscriptionStatus.Expired;
        }

        // 创建新订阅
        var subscription = new Subscription
        {
            DealerId = dealerId,
            StartTime = DateTime.UtcNow,
            EndTime = request.NewEndTime,
            Amount = request.Amount,
            Status = SubscriptionStatus.Active
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsSubscriptionValidAsync(int dealerId)
    {
        return await _db.Subscriptions
            .AnyAsync(s => s.DealerId == dealerId 
                && s.Status == SubscriptionStatus.Active 
                && s.EndTime > DateTime.UtcNow);
    }
}
