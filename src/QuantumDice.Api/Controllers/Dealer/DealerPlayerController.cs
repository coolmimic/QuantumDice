using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumDice.Api.DTOs;
using QuantumDice.Api.Services;

namespace QuantumDice.Api.Controllers.Dealer;

/// <summary>
/// 庄家 - 玩家管理接口
/// </summary>
[ApiController]
[Route("api/dealer/players")]
[Authorize(Roles = "Dealer,Admin")]
public class DealerPlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public DealerPlayerController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    /// <summary>
    /// 获取群组内所有玩家
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PlayerDto>>>> GetPlayers([FromQuery] long groupId)
    {
        var players = await _playerService.GetPlayersByGroupAsync(groupId);
        return Ok(new ApiResponse<List<PlayerDto>>(true, null, players));
    }

    /// <summary>
    /// 获取单个玩家详情
    /// </summary>
    [HttpGet("{playerId}")]
    public async Task<ActionResult<ApiResponse<PlayerDto>>> GetPlayer(long playerId)
    {
        var player = await _playerService.GetPlayerAsync(playerId);
        if (player == null)
            return NotFound(new ApiResponse<PlayerDto>(false, "玩家不存在", null));

        return Ok(new ApiResponse<PlayerDto>(true, null, player));
    }

    /// <summary>
    /// 上分 (充值)
    /// </summary>
    [HttpPost("{playerId}/deposit")]
    public async Task<ActionResult<ApiResponse>> Deposit(long playerId, [FromBody] AdjustBalanceRequest request, [FromQuery] int operatorId)
    {
        var result = await _playerService.AdjustBalanceAsync(
            playerId, 
            Math.Abs(request.Amount), 
            request.Remark, 
            operatorId, 
            isDeposit: true
        );

        if (!result)
            return NotFound(new ApiResponse(false, "玩家不存在"));

        return Ok(new ApiResponse(true, "上分成功"));
    }

    /// <summary>
    /// 下分 (提现)
    /// </summary>
    [HttpPost("{playerId}/withdraw")]
    public async Task<ActionResult<ApiResponse>> Withdraw(long playerId, [FromBody] AdjustBalanceRequest request, [FromQuery] int operatorId)
    {
        var player = await _playerService.GetPlayerAsync(playerId);
        if (player == null)
            return NotFound(new ApiResponse(false, "玩家不存在"));

        if (player.Balance < request.Amount)
            return BadRequest(new ApiResponse(false, "余额不足"));

        var result = await _playerService.AdjustBalanceAsync(
            playerId, 
            -Math.Abs(request.Amount), 
            request.Remark, 
            operatorId, 
            isDeposit: false
        );

        return Ok(new ApiResponse(true, "下分成功"));
    }

    /// <summary>
    /// 封禁玩家
    /// </summary>
    [HttpPost("{playerId}/ban")]
    public async Task<ActionResult<ApiResponse>> Ban(long playerId)
    {
        var result = await _playerService.BanPlayerAsync(playerId, true);
        if (!result)
            return NotFound(new ApiResponse(false, "玩家不存在"));

        return Ok(new ApiResponse(true, "已封禁"));
    }

    /// <summary>
    /// 解封玩家
    /// </summary>
    [HttpPost("{playerId}/unban")]
    public async Task<ActionResult<ApiResponse>> Unban(long playerId)
    {
        var result = await _playerService.BanPlayerAsync(playerId, false);
        if (!result)
            return NotFound(new ApiResponse(false, "玩家不存在"));

        return Ok(new ApiResponse(true, "已解封"));
    }
}
