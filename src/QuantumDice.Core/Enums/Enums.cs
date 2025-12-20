namespace QuantumDice.Core.Enums;

/// <summary>
/// 游戏轮次状态
/// </summary>
public enum RoundStatus
{
    /// <summary>投注中</summary>
    Betting = 0,
    
    /// <summary>已封盘</summary>
    Closed = 1,
    
    /// <summary>开奖中</summary>
    Drawing = 2,
    
    /// <summary>已结算</summary>
    Settled = 3,
    
    /// <summary>已取消</summary>
    Cancelled = 4
}

/// <summary>
/// 投注状态
/// </summary>
public enum BetStatus
{
    /// <summary>待开奖</summary>
    Pending = 0,
    
    /// <summary>已中奖</summary>
    Won = 1,
    
    /// <summary>未中奖</summary>
    Lost = 2,
    
    /// <summary>已退款</summary>
    Refunded = 3
}

/// <summary>
/// 订阅状态
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>活跃</summary>
    Active = 0,
    
    /// <summary>已过期</summary>
    Expired = 1,
    
    /// <summary>已取消</summary>
    Cancelled = 2
}

/// <summary>
/// 充值状态
/// </summary>
public enum DepositStatus
{
    /// <summary>待处理</summary>
    Pending = 0,
    
    /// <summary>已完成</summary>
    Completed = 1,
    
    /// <summary>已拒绝</summary>
    Rejected = 2
}

/// <summary>
/// 提现状态
/// </summary>
public enum WithdrawalStatus
{
    /// <summary>待处理</summary>
    Pending = 0,
    
    /// <summary>已批准</summary>
    Approved = 1,
    
    /// <summary>已拒绝</summary>
    Rejected = 2
}

/// <summary>
/// 交易类型
/// </summary>
public enum TransactionType
{
    /// <summary>充值</summary>
    Deposit = 0,
    
    /// <summary>提现</summary>
    Withdraw = 1,
    
    /// <summary>投注</summary>
    Bet = 2,
    
    /// <summary>中奖</summary>
    Win = 3,
    
    /// <summary>退款</summary>
    Refund = 4
}

/// <summary>
/// 操作员类型
/// </summary>
public enum OperatorType
{
    /// <summary>超级管理员</summary>
    SuperAdmin = 0,
    
    /// <summary>庄家</summary>
    Dealer = 1
}
