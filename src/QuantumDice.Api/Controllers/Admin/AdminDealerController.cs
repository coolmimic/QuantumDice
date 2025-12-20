using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumDice.Api.DTOs;
using QuantumDice.Api.Services;

namespace QuantumDice.Api.Controllers.Admin;

/// <summary>
/// 超级管理员 - 庄家管理接口
/// </summary>
[ApiController]
[Route("api/admin/dealers")]
[Authorize(Roles = "Admin")]
public class AdminDealerController : ControllerBase
{
    private readonly IDealerService _dealerService;

    public AdminDealerController(IDealerService dealerService)
    {
        _dealerService = dealerService;
    }

    /// <summary>
    /// 获取所有庄家列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DealerDto>>>> GetAll()
    {
        var dealers = await _dealerService.GetAllDealersAsync();
        return Ok(new ApiResponse<List<DealerDto>>(true, null, dealers));
    }

    /// <summary>
    /// 获取单个庄家详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DealerDto>>> GetById(int id)
    {
        var dealer = await _dealerService.GetDealerByIdAsync(id);
        if (dealer == null)
            return NotFound(new ApiResponse<DealerDto>(false, "庄家不存在", null));

        return Ok(new ApiResponse<DealerDto>(true, null, dealer));
    }

    /// <summary>
    /// 创建新庄家
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DealerDto>>> Create([FromBody] CreateDealerRequest request)
    {
        var dealer = await _dealerService.CreateDealerAsync(request);
        var dto = await _dealerService.GetDealerByIdAsync(dealer.Id);
        return CreatedAtAction(nameof(GetById), new { id = dealer.Id }, 
            new ApiResponse<DealerDto>(true, "创建成功", dto));
    }

    /// <summary>
    /// 更新庄家信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(int id, [FromBody] UpdateDealerRequest request)
    {
        var result = await _dealerService.UpdateDealerAsync(id, request);
        if (!result)
            return NotFound(new ApiResponse(false, "庄家不存在"));

        return Ok(new ApiResponse(true, "更新成功"));
    }

    /// <summary>
    /// 延长庄家订阅
    /// </summary>
    [HttpPost("{id}/extend")]
    public async Task<ActionResult<ApiResponse>> ExtendSubscription(int id, [FromBody] ExtendSubscriptionRequest request)
    {
        var result = await _dealerService.ExtendSubscriptionAsync(id, request);
        if (!result)
            return NotFound(new ApiResponse(false, "庄家不存在"));

        return Ok(new ApiResponse(true, "续费成功"));
    }

    /// <summary>
    /// 停用庄家
    /// </summary>
    [HttpPost("{id}/disable")]
    public async Task<ActionResult<ApiResponse>> Disable(int id)
    {
        var result = await _dealerService.UpdateDealerAsync(id, new UpdateDealerRequest(null, false));
        if (!result)
            return NotFound(new ApiResponse(false, "庄家不存在"));

        return Ok(new ApiResponse(true, "已停用"));
    }

    /// <summary>
    /// 启用庄家
    /// </summary>
    [HttpPost("{id}/enable")]
    public async Task<ActionResult<ApiResponse>> Enable(int id)
    {
        var result = await _dealerService.UpdateDealerAsync(id, new UpdateDealerRequest(null, true));
        if (!result)
            return NotFound(new ApiResponse(false, "庄家不存在"));

        return Ok(new ApiResponse(true, "已启用"));
    }
}
