using Microsoft.AspNetCore.Mvc;
using QuantumDice.Api.DTOs;
using QuantumDice.Api.Services;

namespace QuantumDice.Api.Controllers;

/// <summary>
/// 认证接口
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// 超管登录
    /// </summary>
    [HttpPost("admin/login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> AdminLogin([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAdminAsync(request);
        if (result == null)
            return Unauthorized(new ApiResponse<LoginResponse>(false, "用户名或密码错误", null));

        return Ok(new ApiResponse<LoginResponse>(true, "登录成功", result));
    }

    /// <summary>
    /// 庄家登录
    /// </summary>
    [HttpPost("dealer/login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> DealerLogin([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginDealerAsync(request);
        if (result == null)
            return Unauthorized(new ApiResponse<LoginResponse>(false, "用户名或密码错误，或订阅已过期", null));

        return Ok(new ApiResponse<LoginResponse>(true, "登录成功", result));
    }

    /// <summary>
    /// 验证 Token
    /// </summary>
    [HttpPost("verify")]
    public ActionResult<ApiResponse<bool>> VerifyToken([FromHeader(Name = "Authorization")] string? authorization)
    {
        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
            return Unauthorized(new ApiResponse<bool>(false, "Token 缺失", false));

        var token = authorization.Substring("Bearer ".Length);
        var principal = _authService.ValidateToken(token);

        if (principal == null)
            return Unauthorized(new ApiResponse<bool>(false, "Token 无效或已过期", false));

        return Ok(new ApiResponse<bool>(true, "Token 有效", true));
    }
}
