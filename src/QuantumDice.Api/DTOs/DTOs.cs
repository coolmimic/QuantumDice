namespace QuantumDice.Api.DTOs;

// ========== 通用响应 ==========
public record ApiResponse<T>(bool Success, string? Message, T? Data);
public record ApiResponse(bool Success, string? Message);

// ========== 认证相关 ==========
public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, string Role, DateTime ExpiresAt);

// ========== 超管 - 庄家管理 ==========
public record CreateDealerRequest(string Username, string Password, string? ContactTelegram, DateTime SubscriptionEndTime);
public record UpdateDealerRequest(string? ContactTelegram, bool? IsActive);
public record ExtendSubscriptionRequest(DateTime NewEndTime, decimal? Amount);

public record DealerDto(
    int Id,
    string Username,
    string? ContactTelegram,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? SubscriptionEndTime,
    int GroupCount
);

// ========== 庄家 - 群组管理 ==========
public record BindGroupRequest(long TelegramGroupId, string? GroupName);
public record GroupDto(
    long Id,
    long TelegramGroupId,
    string? GroupName,
    bool IsActive,
    DateTime BoundAt,
    int PlayerCount
);

// ========== 庄家 - 赔率配置 ==========
public record UpdateOddsRequest(int BetMethodId, decimal? CustomOdds, decimal MinBet, decimal MaxBet, bool IsEnabled);
public record OddsConfigDto(
    int BetMethodId,
    string BetMethodName,
    string BaseGameName,
    string GameTypeName,
    decimal DefaultOdds,
    decimal? CustomOdds,
    decimal MinBet,
    decimal MaxBet,
    bool IsEnabled
);

// ========== 庄家 - 玩家管理 ==========
public record AdjustBalanceRequest(long PlayerId, decimal Amount, string Remark);
public record PlayerDto(
    long Id,
    long TelegramUserId,
    string? Username,
    string? FirstName,
    decimal Balance,
    decimal TotalDeposit,
    decimal TotalWithdraw,
    decimal TotalBet,
    decimal TotalWin,
    bool IsBanned,
    DateTime JoinedAt,
    DateTime? LastActiveAt
);

// ========== 游戏相关 ==========
public record GameTypeDto(int Id, string Code, string Name, int DiceCount, bool IsActive);
public record BaseGameDto(int Id, string Code, string Name, int GameTypeId);
public record BetMethodDto(int Id, string Code, string Name, decimal DefaultOdds, int BaseGameId);

// ========== 投注相关 ==========
public record PlaceBetRequest(long TelegramUserId, long GroupId, string GameCode, string BetContent);
public record BetDto(
    long Id,
    string BetContent,
    decimal Amount,
    decimal Odds,
    decimal WinAmount,
    string Status,
    DateTime CreatedAt
);

// ========== 轮次相关 ==========
public record RoundDto(
    long Id,
    string RoundNumber,
    string GameTypeName,
    string Status,
    DateTime OpenTime,
    DateTime CloseTime,
    DateTime? DrawTime,
    int[] DiceResults
);

// ========== 报表相关 ==========
public record DashboardDto(
    decimal TodayBetAmount,
    decimal TodayWinAmount,
    decimal TodayProfit,
    int TodayBetCount,
    int ActivePlayerCount,
    int TotalPlayerCount
);

public record BetRecordDto(
    long Id,
    string PlayerName,
    string RoundNumber,
    string BetContent,
    decimal Amount,
    decimal WinAmount,
    string Status,
    DateTime CreatedAt
);
