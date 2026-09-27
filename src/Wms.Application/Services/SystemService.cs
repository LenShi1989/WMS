using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;

namespace Wms.Application.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> QueryAsync(UserQuery query, CancellationToken ct = default);
    Task<UserDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<UserDto> CreateAsync(UserSaveDto dto, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(Guid id, UserSaveDto dto, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class UserService(
    IWmsDbContext db,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IAuditService audit) : IUserService
{
    public Task<PagedResult<UserDto>> QueryAsync(UserQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.Users
            .AsNoTracking()
            .WhereIf(query.IsActive.HasValue, u => u.IsActive == query.IsActive)
            .WhereIf(query.RoleId.HasValue, u => u.UserRoles.Any(ur => ur.RoleId == query.RoleId))
            .WhereIf(!string.IsNullOrEmpty(keyword),
                u => u.Username.ToLower().Contains(keyword)
                     || u.DisplayName.ToLower().Contains(keyword)
                     || (u.Email != null && u.Email.ToLower().Contains(keyword)))
            .OrderBy(u => u.Username);

        return q.ToPagedResultAsync(query, Projection, ct);
    }

    public async Task<UserDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.Users.AsNoTracking().Where(u => u.Id == id).Select(Projection).FirstOrDefaultAsync(ct);
        return dto ?? throw AppException.NotFound("找不到指定的使用者。");
    }

    public async Task<UserDto> CreateAsync(UserSaveDto dto, CancellationToken ct = default)
    {
        var username = dto.Username.Trim();

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new AppException("建立使用者時必須設定密碼。", "PASSWORD_REQUIRED");
        }

        if (await db.Users.AnyAsync(u => u.Username == username, ct))
        {
            throw AppException.Conflict($"帳號 {username} 已存在。", "DUPLICATE_USERNAME");
        }

        await EnsureRolesExistAsync(dto.RoleIds, ct);

        var user = new User
        {
            Username = username,
            DisplayName = dto.DisplayName.Trim(),
            Email = dto.Email,
            PasswordHash = passwordHasher.Hash(dto.Password),
            IsActive = dto.IsActive
        };

        foreach (var roleId in dto.RoleIds.Distinct())
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "USER", "User", user.Id, user.Username,
            null, new { dto.Username, dto.DisplayName, dto.IsActive }, ct);

        return await GetAsync(user.Id, ct);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UserSaveDto dto, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的使用者。");

        var username = dto.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Username == username && u.Id != id, ct))
        {
            throw AppException.Conflict($"帳號 {username} 已存在。", "DUPLICATE_USERNAME");
        }

        if (user.Id == currentUser.UserId && !dto.IsActive)
        {
            throw new AppException("不可停用自己的帳號。", "SELF_DEACTIVATE");
        }

        await EnsureRolesExistAsync(dto.RoleIds, ct);

        user.Username = username;
        user.DisplayName = dto.DisplayName.Trim();
        user.Email = dto.Email;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = passwordHasher.Hash(dto.Password);
        }

        var targetRoleIds = dto.RoleIds.Distinct().ToHashSet();
        var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();

        foreach (var removed in user.UserRoles.Where(ur => !targetRoleIds.Contains(ur.RoleId)).ToList())
        {
            db.UserRoles.Remove(removed);
        }

        foreach (var added in targetRoleIds.Except(currentRoleIds))
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = added });
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("UPDATE", "USER", "User", user.Id, user.Username,
            null, new { dto.Username, dto.DisplayName, dto.IsActive }, ct);

        return await GetAsync(id, ct);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的使用者。");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("RESET_PASSWORD", "USER", "User", user.Id, user.Username, null, null, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的使用者。");

        if (user.Id == currentUser.UserId)
        {
            throw new AppException("不可刪除自己的帳號。", "SELF_DELETE");
        }

        // 保留歷史紀錄的完整性，改以停用取代實體刪除。
        user.IsActive = false;
        user.RefreshToken = null;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("DEACTIVATE", "USER", "User", user.Id, user.Username, null, null, ct);
    }

    private async Task EnsureRolesExistAsync(List<Guid> roleIds, CancellationToken ct)
    {
        if (roleIds.Count == 0)
        {
            return;
        }

        var found = await db.Roles.CountAsync(r => roleIds.Contains(r.Id), ct);
        if (found != roleIds.Distinct().Count())
        {
            throw AppException.NotFound("角色清單中有不存在的角色。");
        }
    }

    private static readonly System.Linq.Expressions.Expression<Func<User, UserDto>> Projection =
        u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            DisplayName = u.DisplayName,
            Email = u.Email,
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt,
            Roles = u.UserRoles.Select(ur => new RoleBriefDto { Id = ur.RoleId, Name = ur.Role.Name }).ToList()
        };
}

public interface IRoleService
{
    Task<PagedResult<RoleDto>> QueryAsync(PagedQuery query, CancellationToken ct = default);
    Task<RoleDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<RoleDto> CreateAsync(RoleSaveDto dto, CancellationToken ct = default);
    Task<RoleDto> UpdateAsync(Guid id, RoleSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<List<PermissionGroupDto>> GetPermissionsAsync(CancellationToken ct = default);
}

public class RoleService(IWmsDbContext db, IAuditService audit) : IRoleService
{
    public Task<PagedResult<RoleDto>> QueryAsync(PagedQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.Roles
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(keyword), r => r.Name.ToLower().Contains(keyword))
            .OrderBy(r => r.Name);

        return q.ToPagedResultAsync(query, Projection, ct);
    }

    public async Task<RoleDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await db.Roles.AsNoTracking().Where(r => r.Id == id).Select(Projection).FirstOrDefaultAsync(ct);
        return dto ?? throw AppException.NotFound("找不到指定的角色。");
    }

    public async Task<RoleDto> CreateAsync(RoleSaveDto dto, CancellationToken ct = default)
    {
        var name = dto.Name.Trim();
        if (await db.Roles.AnyAsync(r => r.Name == name, ct))
        {
            throw AppException.Conflict($"角色名稱 {name} 已存在。", "DUPLICATE_NAME");
        }

        var role = new Role
        {
            Name = name,
            Description = dto.Description
        };

        await ApplyPermissionsAsync(role, dto.PermissionIds, ct);

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CREATE", "ROLE", "Role", role.Id, role.Name, null, dto, ct);

        return await GetAsync(role.Id, ct);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, RoleSaveDto dto, CancellationToken ct = default)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的角色。");

        var name = dto.Name.Trim();
        if (await db.Roles.AnyAsync(r => r.Name == name && r.Id != id, ct))
        {
            throw AppException.Conflict($"角色名稱 {name} 已存在。", "DUPLICATE_NAME");
        }

        if (role.IsSystem && role.Name != name)
        {
            throw new AppException("系統內建角色不可更名。", "SYSTEM_ROLE");
        }

        role.Name = name;
        role.Description = dto.Description;

        var target = dto.PermissionIds.Distinct().ToHashSet();
        var current = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

        foreach (var removed in role.RolePermissions.Where(rp => !target.Contains(rp.PermissionId)).ToList())
        {
            db.RolePermissions.Remove(removed);
        }

        var toAdd = target.Except(current).ToList();
        if (toAdd.Count > 0)
        {
            var valid = await db.Permissions.CountAsync(p => toAdd.Contains(p.Id), ct);
            if (valid != toAdd.Count)
            {
                throw AppException.NotFound("權限清單中有不存在的權限。");
            }

            foreach (var permissionId in toAdd)
            {
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
            }
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("UPDATE", "ROLE", "Role", role.Id, role.Name, null, dto, ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw AppException.NotFound("找不到指定的角色。");

        if (role.IsSystem)
        {
            throw new AppException("系統內建角色不可刪除。", "SYSTEM_ROLE");
        }

        if (await db.UserRoles.AnyAsync(ur => ur.RoleId == id, ct))
        {
            throw AppException.Conflict("仍有使用者屬於此角色，無法刪除。", "IN_USE");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("DELETE", "ROLE", "Role", id, role.Name, role.Name, null, ct);
    }

    public async Task<List<PermissionGroupDto>> GetPermissionsAsync(CancellationToken ct = default)
    {
        var permissions = await db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Code)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Module = p.Module,
                Action = p.Action
            })
            .ToListAsync(ct);

        return [.. permissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto { Module = g.Key, Permissions = [.. g] })];
    }

    private async Task ApplyPermissionsAsync(Role role, List<Guid> permissionIds, CancellationToken ct)
    {
        var ids = permissionIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var valid = await db.Permissions.CountAsync(p => ids.Contains(p.Id), ct);
        if (valid != ids.Count)
        {
            throw AppException.NotFound("權限清單中有不存在的權限。");
        }

        foreach (var permissionId in ids)
        {
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }
    }

    private static readonly System.Linq.Expressions.Expression<Func<Role, RoleDto>> Projection =
        r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsSystem = r.IsSystem,
            UserCount = r.UserRoles.Count,
            Permissions = r.RolePermissions.Select(rp => rp.Permission.Code).ToList(),
            CreatedAt = r.CreatedAt
        };
}

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> QueryAsync(AuditLogQuery query, CancellationToken ct = default);
}

public class AuditLogService(IWmsDbContext db) : IAuditLogService
{
    public Task<PagedResult<AuditLogDto>> QueryAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var keyword = query.Keyword?.Trim().ToLowerInvariant();

        var q = db.AuditLogs
            .AsNoTracking()
            .WhereIf(query.UserId.HasValue, a => a.UserId == query.UserId)
            .WhereIf(!string.IsNullOrEmpty(query.Module), a => a.Module == query.Module)
            .WhereIf(!string.IsNullOrEmpty(query.Action), a => a.Action == query.Action)
            .WhereIf(query.DateFrom.HasValue, a => a.CreatedAt >= query.DateFrom!.Value)
            .WhereIf(query.DateTo.HasValue, a => a.CreatedAt < query.DateTo!.Value.AddDays(1))
            .WhereIf(!string.IsNullOrEmpty(keyword),
                a => (a.Username != null && a.Username.ToLower().Contains(keyword))
                     || (a.ReferenceNo != null && a.ReferenceNo.ToLower().Contains(keyword)))
            .OrderByDescending(a => a.CreatedAt);

        return q.ToPagedResultAsync(query, a => new AuditLogDto
        {
            Id = a.Id,
            UserId = a.UserId,
            Username = a.Username,
            Action = a.Action,
            Module = a.Module,
            ReferenceType = a.ReferenceType,
            ReferenceId = a.ReferenceId,
            ReferenceNo = a.ReferenceNo,
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            IpAddress = a.IpAddress,
            CreatedAt = a.CreatedAt
        }, ct);
    }
}
