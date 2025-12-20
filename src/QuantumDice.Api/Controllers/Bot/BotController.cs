using Microsoft.AspNetCore.Mvc;
using QuantumDice.Api.DTOs;
using QuantumDice.Api.Services;

namespace QuantumDice.Api.Controllers.Bot;

/// <summary>
/// Bot 回调接口 - 供 Telegram Bot 调用
/// </summary>
[ApiController]
[Route("api/bot")]
public class BotController : ControllerBase
{
    private readonly IPlayerService _playerService;
    private readonly IGameService _gameService;
    private readonly IDealerService _dealerService;

    public BotController(IPlayerService playerService, IGameService gameService, IDealerService dealerService)
    {
        _playerService = playerService;
        _gameService = gameService;
        _dealerService = dealerService;
    }

    /// <summary>
    /// 获取游戏类型列表
    /// </summary>
    [HttpGet("games")]
    public async Task<ActionResult<ApiResponse<List<GameTypeDto>>>> GetGameTypes()
    {
        var games = await _gameService.GetGameTypesAsync();
        return Ok(new ApiResponse<List<GameTypeDto>>(true, null, games));
    }

    /// <summary>
    /// 获取或创建玩家
    /// </summary>
    [HttpPost("players/sync")]
    public async Task<ActionResult<ApiResponse<PlayerDto>>> SyncPlayer(
        [FromQuery] long telegramUserId,
        [FromQuery] long groupId,
        [FromQuery] string? username,
        [FromQuery] string? firstName)
    {
        var player = await _playerService.GetOrCreatePlayerAsync(telegramUserId, groupId, username, firstName);
        var dto = await _playerService.GetPlayerAsync(player.Id);
        return Ok(new ApiResponse<PlayerDto>(true, null, dto));
    }

    /// <summary>
    /// 查询玩家余额
    /// </summary>
    [HttpGet("players/{telegramUserId}/balance")]
    public async Task<ActionResult<ApiResponse<decimal>>> GetBalance(long telegramUserId, [FromQuery] long groupId)
    {
        var player = await _playerService.GetOrCreatePlayerAsync(telegramUserId, groupId, null, null);
        return Ok(new ApiResponse<decimal>(true, null, player.Balance));
    }

    /// <summary>
    /// 获取当前游戏轮次
    /// </summary>
    [HttpGet("rounds/current")]
    public async Task<ActionResult<ApiResponse<RoundDto>>> GetCurrentRound([FromQuery] long groupId, [FromQuery] int gameTypeId)
    {
        var round = await _gameService.GetCurrentRoundAsync(groupId, gameTypeId);
        if (round == null)
            return NotFound(new ApiResponse<RoundDto>(false, "暂无进行中的轮次", null));

        var dto = new RoundDto(
            round.Id,
            round.RoundNumber,
            "",
            round.Status.ToString(),
            round.OpenTime,
            round.CloseTime,
            round.DrawTime,
            Array.Empty<int>()
        );

        return Ok(new ApiResponse<RoundDto>(true, null, dto));
    }

    /// <summary>
    /// 开始新一轮游戏
    /// </summary>
    [HttpPost("rounds/start")]
    public async Task<ActionResult<ApiResponse<RoundDto>>> StartRound(
        [FromQuery] long groupId,
        [FromQuery] int gameTypeId,
        [FromQuery] int intervalMinutes = 5)
    {
        var round = await _gameService.StartNewRoundAsync(groupId, gameTypeId, intervalMinutes);
        
        var dto = new RoundDto(
            round.Id,
            round.RoundNumber,
            "",
            round.Status.ToString(),
            round.OpenTime,
            round.CloseTime,
            round.DrawTime,
            Array.Empty<int>()
        );

        return Ok(new ApiResponse<RoundDto>(true, "新轮次已开始", dto));
    }

    /// <summary>
    /// 开奖
    /// </summary>
    [HttpPost("rounds/{roundId}/draw")]
    public async Task<ActionResult<ApiResponse<int[]>>> Draw(long roundId, [FromQuery] int diceCount)
    {
        var results = await _gameService.DrawDiceAsync(roundId, diceCount);
        return Ok(new ApiResponse<int[]>(true, null, results.ToArray()));
    }

    /// <summary>
    /// 结算
    /// </summary>
    [HttpPost("rounds/{roundId}/settle")]
    public async Task<ActionResult<ApiResponse>> Settle(long roundId)
    {
        await _gameService.SettleRoundAsync(roundId);
        return Ok(new ApiResponse(true, "结算完成"));
    }

    /// <summary>
    /// 检查庄家订阅状态
    /// </summary>
    [HttpGet("subscription/check")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckSubscription([FromQuery] int dealerId)
    {
        var isValid = await _dealerService.IsSubscriptionValidAsync(dealerId);
        return Ok(new ApiResponse<bool>(true, null, isValid));
    }
}
