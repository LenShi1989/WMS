using System.ComponentModel.DataAnnotations;
using Wms.Application.Common;

namespace Wms.Application.Dtos;

// ---------- 使用者 ----------
public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RoleBriefDto> Roles { get; set; } = [];
}

public class UserSaveDto
{
    [Required, MaxLength(50)] public string Username { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string DisplayName { get; set; } = string.Empty;
    [EmailAddress, MaxLength(200)] public string? Email { get; set; }

    /// <summary>新增時必填；修改時留空表示不變更密碼。</summary>
    [MinLength(8)] public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
    public List<Guid> RoleIds { get; set; } = [];
}

public class UserQuery : PagedQuery
{
    public bool? IsActive { get; set; }
    public Guid? RoleId { get; set; }
}

public class ResetPasswordRequest
{
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}

// ---------- 角色 ----------
public class RoleBriefDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
    public List<string> Permissions { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class RoleSaveDto
{
    [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;
    [MaxLength(200)] public string? Description { get; set; }
    public List<Guid> PermissionIds { get; set; } = [];
}

// ---------- 權限 ----------
public class PermissionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

/// <summary>依模組分組的權限清單，方便前端渲染勾選畫面。</summary>
public class PermissionGroupDto
{
    public string Module { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = [];
}

// ---------- 操作紀錄 ----------
public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogQuery : PagedQuery
{
    public Guid? UserId { get; set; }
    public string? Module { get; set; }
    public string? Action { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

// ---------- Dashboard ----------
public class DashboardSummaryDto
{
    public decimal InboundToday { get; set; }
    public decimal OutboundToday { get; set; }
    public decimal InventoryTotal { get; set; }
    public int PendingPutaway { get; set; }
    public int PendingPicking { get; set; }
    public int PendingStocktake { get; set; }
    public int MaterialCount { get; set; }
    public int WarehouseCount { get; set; }
    public int LocationCount { get; set; }
    public int BelowSafetyStockCount { get; set; }
    public int OpenInboundOrders { get; set; }
    public int OpenOutboundOrders { get; set; }
}

/// <summary>近 N 日出入庫趨勢。</summary>
public class DashboardTrendDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Inbound { get; set; }
    public decimal Outbound { get; set; }
}

/// <summary>倉庫庫存佔比。</summary>
public class WarehouseStockDto
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
