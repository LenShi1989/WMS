using Microsoft.Extensions.DependencyInjection;
using Wms.Application.Services;

namespace Wms.Application;

public static class DependencyInjection
{
    /// <summary>註冊所有 Use Case 服務。</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IInventoryEngine, InventoryEngine>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<IMaterialCategoryService, MaterialCategoryService>();
        services.AddScoped<IUomService, UomService>();
        services.AddScoped<IBarcodeService, BarcodeService>();

        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<ILocationService, LocationService>();

        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInboundService, InboundService>();
        services.AddScoped<IPutawayService, PutawayService>();
        services.AddScoped<IOutboundService, OutboundService>();
        services.AddScoped<IPickingService, PickingService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IStocktakeService, StocktakeService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
