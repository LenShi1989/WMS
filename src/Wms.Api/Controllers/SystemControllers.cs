using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Services;

namespace Wms.Api.Controllers;

/// <summary>使用者管理。</summary>
public class UsersController(IUserService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.UserView)]
    public async Task<ApiResponse<PagedResult<UserDto>>> Query([FromQuery] UserQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.UserView)]
    public async Task<ApiResponse<UserDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.UserManage)]
    public async Task<ApiResponse<UserDto>> Create(UserSaveDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "使用者已建立。");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.UserManage)]
    public async Task<ApiResponse<UserDto>> Update(Guid id, UserSaveDto dto, CancellationToken ct)
        => Success(await service.UpdateAsync(id, dto, ct), "使用者已更新。");

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = Permissions.UserManage)]
    public async Task<ApiResponse> ResetPassword(Guid id, ResetPasswordRequest request, CancellationToken ct)
    {
        await service.ResetPasswordAsync(id, request, ct);
        return Success("密碼已重設。");
    }

    /// <summary>停用使用者（保留歷史紀錄，不做實體刪除）。</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.UserManage)]
    public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Success("使用者已停用。");
    }
}

/// <summary>角色與權限。</summary>
public class RolesController(IRoleService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.RoleView)]
    public async Task<ApiResponse<PagedResult<RoleDto>>> Query([FromQuery] PagedQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    /// <summary>依模組分組的全部權限清單。</summary>
    [HttpGet("permissions")]
    [Authorize(Policy = Permissions.RoleView)]
    public async Task<ApiResponse<List<PermissionGroupDto>>> GetPermissions(CancellationToken ct)
        => Success(await service.GetPermissionsAsync(ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.RoleView)]
    public async Task<ApiResponse<RoleDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.RoleManage)]
    public async Task<ApiResponse<RoleDto>> Create(RoleSaveDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "角色已建立。");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.RoleManage)]
    public async Task<ApiResponse<RoleDto>> Update(Guid id, RoleSaveDto dto, CancellationToken ct)
        => Success(await service.UpdateAsync(id, dto, ct), "角色已更新。");

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.RoleManage)]
    public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Success("角色已刪除。");
    }
}

/// <summary>操作紀錄，唯讀。</summary>
[Route("api/v1/audit-logs")]
public class AuditLogsController(IAuditLogService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.AuditView)]
    public async Task<ApiResponse<PagedResult<AuditLogDto>>> Query([FromQuery] AuditLogQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));
}

/// <summary>儀表板統計。</summary>
public class DashboardController(IDashboardService service) : ApiControllerBase
{
    /// <summary>今日出入庫、待辦任務與庫存總覽。</summary>
    [HttpGet("summary")]
    [Authorize(Policy = Permissions.DashboardView)]
    public async Task<ApiResponse<DashboardSummaryDto>> Summary(CancellationToken ct)
        => Success(await service.GetSummaryAsync(ct));

    /// <summary>近 N 日出入庫趨勢，預設 7 天。</summary>
    [HttpGet("trend")]
    [Authorize(Policy = Permissions.DashboardView)]
    public async Task<ApiResponse<List<DashboardTrendDto>>> Trend([FromQuery] int days = 7, CancellationToken ct = default)
        => Success(await service.GetTrendAsync(days, ct));

    /// <summary>各倉庫庫存量佔比。</summary>
    [HttpGet("warehouse-stock")]
    [Authorize(Policy = Permissions.DashboardView)]
    public async Task<ApiResponse<List<WarehouseStockDto>>> WarehouseStock(CancellationToken ct)
        => Success(await service.GetWarehouseStockAsync(ct));

    /// <summary>低於安全庫存的物料清單。</summary>
    [HttpGet("low-stock")]
    [Authorize(Policy = Permissions.DashboardView)]
    public async Task<ApiResponse<List<InventorySummaryDto>>> LowStock([FromQuery] int top = 10, CancellationToken ct = default)
        => Success(await service.GetLowStockAsync(top, ct));
}
