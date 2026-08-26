namespace CrmLeadTool.Api.DTOs;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserDto User { get; set; } = new();
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class AdminResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}

public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "SALES_REP"; // "ADMIN" or "SALES_REP"
}

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "SALES_REP";
    public bool IsActive { get; set; } = true;
}

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "SALES_REP";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AuditLogDto
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CreateProductRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Pricing { get; set; }
    public string? Features { get; set; }
    public string? Specifications { get; set; }
    public int? CategoryId { get; set; }
}

public class UpdateProductRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Pricing { get; set; }
    public string? Features { get; set; }
    public string? Specifications { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public int? CategoryId { get; set; }
}

public class CreateCategoryRequestDto
{
    public string CategoryName { get; set; } = string.Empty;
}

public class UpdateCategoryRequestDto
{
    public string CategoryName { get; set; } = string.Empty;
}
