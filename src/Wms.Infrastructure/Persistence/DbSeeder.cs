using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wms.Application.Common;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Domain.Enums;

namespace Wms.Infrastructure.Persistence;

/// <summary>
/// 初始資料：權限、角色、管理員帳號，以及一組可直接操作的示範倉庫與物料。
/// 每一段都會先檢查是否已存在，重複執行是安全的。
/// </summary>
public class DbSeeder(WmsDbContext db, IPasswordHasher passwordHasher, ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        var roles = await SeedRolesAsync(ct);
        await SeedAdminUserAsync(roles, ct);
        await SeedMasterDataAsync(ct);

        logger.LogInformation("WMS 初始資料檢查完成。");
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var existing = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
        var missing = Permissions.All.Where(p => !existing.Contains(p.Code)).ToList();

        if (missing.Count == 0)
        {
            return;
        }

        db.Permissions.AddRange(missing.Select(p => new Permission
        {
            Code = p.Code,
            Name = p.Name,
            Module = p.Module,
            Action = p.Action
        }));

        await db.SaveChangesAsync(ct);
        logger.LogInformation("新增 {Count} 筆權限定義。", missing.Count);
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(CancellationToken ct)
    {
        var permissions = await db.Permissions.ToDictionaryAsync(p => p.Code, ct);

        var definitions = new (string Name, string Description, IEnumerable<string> Codes)[]
        {
            (SystemRoles.Admin, "擁有全系統權限", Permissions.AllCodes),

            (SystemRoles.WarehouseManager, "倉儲日常作業與審核", [
                Permissions.DashboardView,
                Permissions.MaterialView, Permissions.MaterialCreate, Permissions.MaterialUpdate,
                Permissions.CategoryView, Permissions.CategoryManage,
                Permissions.UomView, Permissions.UomManage,
                Permissions.BarcodeView, Permissions.BarcodeManage,
                Permissions.WarehouseView, Permissions.WarehouseManage,
                Permissions.ZoneView, Permissions.ZoneManage,
                Permissions.LocationView, Permissions.LocationManage,
                Permissions.InboundView, Permissions.InboundCreate, Permissions.InboundReceive, Permissions.InboundComplete,
                Permissions.PutawayView, Permissions.PutawayExecute,
                Permissions.OutboundView, Permissions.OutboundCreate, Permissions.OutboundAllocate, Permissions.OutboundShip,
                Permissions.PickingView, Permissions.PickingExecute,
                Permissions.InventoryView, Permissions.InventoryAdjust,
                Permissions.TransferView, Permissions.TransferExecute,
                Permissions.StocktakeView, Permissions.StocktakeCreate, Permissions.StocktakeCount, Permissions.StocktakeApprove,
                Permissions.AuditView
            ]),

            (SystemRoles.Operator, "現場收貨、上架、揀貨作業", [
                Permissions.DashboardView,
                Permissions.MaterialView, Permissions.BarcodeView,
                Permissions.WarehouseView, Permissions.ZoneView, Permissions.LocationView,
                Permissions.InboundView, Permissions.InboundReceive,
                Permissions.PutawayView, Permissions.PutawayExecute,
                Permissions.OutboundView,
                Permissions.PickingView, Permissions.PickingExecute,
                Permissions.InventoryView,
                Permissions.TransferView, Permissions.TransferExecute,
                Permissions.StocktakeView, Permissions.StocktakeCount
            ]),

            (SystemRoles.Viewer, "僅可檢視資料", [
                Permissions.DashboardView,
                Permissions.MaterialView, Permissions.CategoryView, Permissions.UomView, Permissions.BarcodeView,
                Permissions.WarehouseView, Permissions.ZoneView, Permissions.LocationView,
                Permissions.InboundView, Permissions.PutawayView,
                Permissions.OutboundView, Permissions.PickingView,
                Permissions.InventoryView, Permissions.TransferView, Permissions.StocktakeView
            ])
        };

        var result = new Dictionary<string, Role>();

        foreach (var definition in definitions)
        {
            var role = await db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Name == definition.Name, ct);

            if (role is null)
            {
                role = new Role
                {
                    Name = definition.Name,
                    Description = definition.Description,
                    IsSystem = true
                };
                db.Roles.Add(role);
            }

            var current = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

            foreach (var code in definition.Codes)
            {
                if (permissions.TryGetValue(code, out var permission) && !current.Contains(permission.Id))
                {
                    role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
                }
            }

            result[definition.Name] = role;
        }

        await db.SaveChangesAsync(ct);
        return result;
    }

    private async Task SeedAdminUserAsync(Dictionary<string, Role> roles, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Username == "admin", ct))
        {
            return;
        }

        var admin = new User
        {
            Username = "admin",
            DisplayName = "系統管理員",
            Email = "admin@wms.local",
            PasswordHash = passwordHasher.Hash("a12345678"),
            IsActive = true
        };

        admin.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = roles[SystemRoles.Admin].Id });

        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("已建立預設管理員帳號 admin。");
    }

    private async Task SeedMasterDataAsync(CancellationToken ct)
    {
        if (await db.Warehouses.AnyAsync(ct))
        {
            return;
        }

        // 單位
        db.UnitOfMeasures.AddRange(
            new UnitOfMeasure { Code = "PCS", Name = "個" },
            new UnitOfMeasure { Code = "BOX", Name = "箱" },
            new UnitOfMeasure { Code = "KG", Name = "公斤" },
            new UnitOfMeasure { Code = "M", Name = "公尺" },
            new UnitOfMeasure { Code = "L", Name = "公升" });

        // 物料分類
        var raw = new MaterialCategory { Code = "RAW", Name = "原物料" };
        var part = new MaterialCategory { Code = "PART", Name = "零組件" };
        var semi = new MaterialCategory { Code = "SEMI", Name = "半成品" };
        var finished = new MaterialCategory { Code = "FG", Name = "成品" };
        db.MaterialCategories.AddRange(raw, part, semi, finished);

        // 倉庫與儲區
        var warehouse = new Warehouse { Code = "WH01", Name = "主倉庫", Description = "廠內主要儲存倉庫" };
        db.Warehouses.Add(warehouse);

        var receivingZone = new WarehouseZone
        {
            WarehouseId = warehouse.Id, Code = "RCV", Name = "收貨區", ZoneType = ZoneType.RECEIVING
        };
        var storageZone = new WarehouseZone
        {
            WarehouseId = warehouse.Id, Code = "STG", Name = "儲存區", ZoneType = ZoneType.STORAGE
        };
        var pickingZone = new WarehouseZone
        {
            WarehouseId = warehouse.Id, Code = "PCK", Name = "揀貨區", ZoneType = ZoneType.PICKING
        };
        var shippingZone = new WarehouseZone
        {
            WarehouseId = warehouse.Id, Code = "SHP", Name = "出貨區", ZoneType = ZoneType.SHIPPING
        };
        db.WarehouseZones.AddRange(receivingZone, storageZone, pickingZone, shippingZone);

        // 儲位
        db.Locations.Add(new Location
        {
            WarehouseId = warehouse.Id,
            ZoneId = receivingZone.Id,
            Code = "WH01-RCV-01",
            Name = "收貨暫存區",
            LocationType = LocationType.STAGING,
            Capacity = 9999
        });

        db.Locations.Add(new Location
        {
            WarehouseId = warehouse.Id,
            ZoneId = shippingZone.Id,
            Code = "WH01-SHP-01",
            Name = "出貨暫存區",
            LocationType = LocationType.STAGING,
            Capacity = 9999
        });

        // 儲存區 A01 ~ A02 各 3 層，共 6 個儲位。
        foreach (var rack in new[] { "A01", "A02" })
        {
            for (var level = 1; level <= 3; level++)
            {
                db.Locations.Add(new Location
                {
                    WarehouseId = warehouse.Id,
                    ZoneId = storageZone.Id,
                    Code = $"WH01-{rack}-R01-L{level:D2}",
                    Name = $"{rack} 第 {level} 層",
                    LocationType = LocationType.SHELF,
                    Capacity = 1000,
                    WeightLimit = 500
                });
            }
        }

        db.Locations.Add(new Location
        {
            WarehouseId = warehouse.Id,
            ZoneId = pickingZone.Id,
            Code = "WH01-PCK-01",
            Name = "揀貨暫存區",
            LocationType = LocationType.STAGING,
            Capacity = 2000
        });

        // 示範物料
        var materials = new[]
        {
            new Material { Code = "MAT001", Name = "六角螺絲 M8x20", Specification = "不鏽鋼 SUS304", CategoryId = part.Id, BaseUom = "PCS", SafetyStock = 500, MinStock = 200, MaxStock = 5000 },
            new Material { Code = "MAT002", Name = "培林軸承 6204", Specification = "內徑 20mm", CategoryId = part.Id, BaseUom = "PCS", SafetyStock = 100, MinStock = 50, MaxStock = 1000 },
            new Material { Code = "MAT003", Name = "鋁擠型 2020", Specification = "長度 6M", CategoryId = raw.Id, BaseUom = "M", SafetyStock = 200, MinStock = 100, MaxStock = 2000 },
            new Material { Code = "MAT004", Name = "控制箱組件", Specification = "含端子台與配線", CategoryId = semi.Id, BaseUom = "PCS", SafetyStock = 20, MinStock = 10, MaxStock = 200 },
            new Material { Code = "MAT005", Name = "自動化輸送機", Specification = "1.5M 皮帶式", CategoryId = finished.Id, BaseUom = "PCS", SafetyStock = 5, MinStock = 2, MaxStock = 50 }
        };
        db.Materials.AddRange(materials);

        foreach (var material in materials)
        {
            db.MaterialBarcodes.Add(new MaterialBarcode
            {
                MaterialId = material.Id,
                Barcode = $"471{material.Code[3..]}00001",
                BarcodeType = "EAN13",
                IsPrimary = true
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("已建立示範倉庫、儲位與物料主檔。");
    }
}
