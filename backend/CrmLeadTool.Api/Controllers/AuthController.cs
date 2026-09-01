using System.Security.Claims;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _authService.LoginAsync(dto.Email, dto.Password, ip);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _authService.RefreshTokenAsync(dto.RefreshToken, ip);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private (string? Role, int? UserId) ExtractUserClaims()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        int? userId = int.TryParse(sub, out int parsed) ? parsed : null;

        if (!string.IsNullOrEmpty(role) && userId.HasValue) return (role, userId);

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var tokenString = authHeader.Substring("Bearer ".Length).Trim();
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(tokenString);
                var tokenRole = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
                var tokenSub = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "nameid")?.Value;
                int? tokenUserId = int.TryParse(tokenSub, out int p) ? p : null;
                return (tokenRole, tokenUserId);
            }
            catch {}
        }

        return (null, null);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        var (_, userId) = ExtractUserClaims();
        await _authService.LogoutAsync(dto.RefreshToken, userId);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        var (_, userId) = ExtractUserClaims();
        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        await _authService.LogoutAllDevicesAsync(userId.Value);
        return Ok(new { message = "Logged out of all devices successfully" });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        var (_, userId) = ExtractUserClaims();
        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            await _authService.ChangePasswordAsync(userId.Value, dto.CurrentPassword, dto.NewPassword);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        try
        {
            var code = await _authService.ForgotPasswordRequestAsync(dto.Email);
            return Ok(new { message = "Password reset request initiated.", code });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        try
        {
            await _authService.ResetPasswordWithCodeAsync(dto.Email, dto.Code, dto.NewPassword);
            return Ok(new { message = "Password has been successfully updated. You may now sign in." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
