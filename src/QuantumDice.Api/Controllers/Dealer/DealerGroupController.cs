using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumDice.Api.DTOs;
using QuantumDice.Api.Services;

namespace QuantumDice.Api.Controllers.Dealer;

/// <summary>
/// 庄家 - 群组管理接口
/// </summary>
[ApiController]
[Route("api/dealer/groups")]
[Authorize(Roles = "Dealer,Admin")]
public class DealerGroupController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly IDistributedCache _cache;

    public DealerGroupController(IGroupService groupService, IDistributedCache cache)
    {
        _groupService = groupService;
        _cache = cache;
    }

    /// <summary>
    /// 生成群组绑定码
    /// </summary>
    [HttpPost("binding-code")]
    public async Task<ActionResult<ApiResponse<string>>> GenerateBindingCode([FromQuery] int dealerId)
    {
        // 简单生成6位数字码
        var code = new Random().Next(100000, 999999).ToString();
        
        // 存入 Redis，有效期 5 分钟
        await _cache.SetStringAsync(
            $"bind_code:{code}", 
            dealerId.ToString(), 
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }
        );

        return Ok(new ApiResponse<string>(true, "生成成功", code));
    }

    /// <summary>
    /// 获取庄家的所有群组
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<GroupDto>>>> GetGroups([FromQuery] int dealerId)
    {
        var groups = await _groupService.GetGroupsByDealerAsync(dealerId);
        return Ok(new ApiResponse<List<GroupDto>>(true, null, groups));
    }

    /// <summary>
    /// 获取群组详情
    /// </summary>
    [HttpGet("{groupId}")]
    public async Task<ActionResult<ApiResponse<GroupDto>>> GetGroup(long groupId)
    {
        var group = await _groupService.GetGroupAsync(groupId);
        if (group == null)
            return NotFound(new ApiResponse<GroupDto>(false, "群组不存在", null));

        return Ok(new ApiResponse<GroupDto>(true, null, group));
    }

    /// <summary>
    /// 绑定群组
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<GroupDto>>> BindGroup([FromQuery] int dealerId, [FromBody] BindGroupRequest request)
    {
        var group = await _groupService.BindGroupAsync(dealerId, request);
        var dto = await _groupService.GetGroupAsync(group.Id);
        return Ok(new ApiResponse<GroupDto>(true, "绑定成功", dto));
    }

    /// <summary>
    /// 解绑群组
    /// </summary>
    [HttpDelete("{groupId}")]
    public async Task<ActionResult<ApiResponse>> UnbindGroup(long groupId)
    {
        var result = await _groupService.UnbindGroupAsync(groupId);
        if (!result)
            return NotFound(new ApiResponse(false, "群组不存在"));

        return Ok(new ApiResponse(true, "解绑成功"));
    }

    /// <summary>
    /// 获取群组赔率配置
    /// </summary>
    [HttpGet("{groupId}/odds")]
    public async Task<ActionResult<ApiResponse<List<OddsConfigDto>>>> GetOddsConfig(long groupId)
    {
        var configs = await _groupService.GetOddsConfigAsync(groupId);
        return Ok(new ApiResponse<List<OddsConfigDto>>(true, null, configs));
    }

    /// <summary>
    /// 更新群组赔率配置
    /// </summary>
    [HttpPut("{groupId}/odds")]
    public async Task<ActionResult<ApiResponse>> UpdateOddsConfig(long groupId, [FromBody] UpdateOddsRequest request)
    {
        var result = await _groupService.UpdateOddsConfigAsync(groupId, request);
        if (!result)
            return NotFound(new ApiResponse(false, "配置不存在"));

        return Ok(new ApiResponse(true, "更新成功"));
    }
}
