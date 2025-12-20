using Microsoft.EntityFrameworkCore;
using QuantumDice.Core.Entities;

namespace QuantumDice.Infrastructure.Data;

public class QuantumDiceDbContext : DbContext
{
    public QuantumDiceDbContext(DbContextOptions<QuantumDiceDbContext> options)
        : base(options)
    {
    }

    // 系统层
    public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

    // 游戏定义层
    public DbSet<GameType> GameTypes => Set<GameType>();
    public DbSet<BaseGame> BaseGames => Set<BaseGame>();
    public DbSet<BetMethod> BetMethods => Set<BetMethod>();

    // 租户层
    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    // 群组层
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupGameConfig> GroupGameConfigs => Set<GroupGameConfig>();
    public DbSet<GroupScheduleConfig> GroupScheduleConfigs => Set<GroupScheduleConfig>();

    // 玩家层
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();

    // 游戏运行层
    public DbSet<GameRound> GameRounds => Set<GameRound>();
    public DbSet<DiceResult> DiceResults => Set<DiceResult>();
    public DbSet<Bet> Bets => Set<Bet>();

    // 流水层
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 应用所有配置
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuantumDiceDbContext).Assembly);
    }
}
