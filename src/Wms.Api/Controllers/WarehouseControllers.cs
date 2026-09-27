using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Services;

namespace Wms.Api.Controllers;

/// <summary>倉庫。</summary>
public class WarehousesController(IWarehouseService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.WarehouseView)]
    public async Task<ApiResponse<PagedResult<WarehouseDto>>> Query([FromQuery] PagedQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.WarehouseView)]
    public async Task<ApiResponse<WarehouseDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.WarehouseManage)]
    public async Task<ApiResponse<WarehouseDto>> Create(WarehouseSaveDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "倉庫已建立。");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.WarehouseManage)]
    public async Task<ApiResponse<WarehouseDto>> Update(Guid id, WarehouseSaveDto dto, CancellationToken ct)
        => Success(await service.UpdateAsync(id, dto, ct), "倉庫已更新。");

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.WarehouseManage)]
    public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Success("倉庫已刪除。");
    }
}

/// <summary>儲區。</summary>
[Route("api/v1/zones")]
public class ZonesController(IZoneService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.ZoneView)]
    public async Task<ApiResponse<PagedResult<ZoneDto>>> Query([FromQuery] ZoneQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ZoneView)]
    public async Task<ApiResponse<ZoneDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.ZoneManage)]
    public async Task<ApiResponse<ZoneDto>> Create(ZoneSaveDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "儲區已建立。");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.ZoneManage)]
    public async Task<ApiResponse<ZoneDto>> Update(Guid id, ZoneSaveDto dto, CancellationToken ct)
        => Success(await service.UpdateAsync(id, dto, ct), "儲區已更新。");

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.ZoneManage)]
    public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Success("儲區已刪除。");
    }
}

/// <summary>儲位。</summary>
public class LocationsController(ILocationService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.LocationView)]
    public async Task<ApiResponse<PagedResult<LocationDto>>> Query([FromQuery] LocationQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    /// <summary>掃描儲位條碼時用編號查詢。</summary>
    [HttpGet("by-code/{code}")]
    [Authorize(Policy = Permissions.LocationView)]
    public async Task<ApiResponse<LocationDto?>> GetByCode(string code, CancellationToken ct)
    {
        var location = await service.GetByCodeAsync(code, ct);
        return location is null
            ? ApiResponse<LocationDto?>.Fail("查無此儲位編號。", new ApiError("NOT_FOUND", code))
            : Success<LocationDto?>(location);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.LocationView)]
    public async Task<ApiResponse<LocationDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.LocationManage)]
    public async Task<ApiResponse<LocationDto>> Create(LocationSaveDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "儲位已建立。");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.LocationManage)]
    public async Task<ApiResponse<LocationDto>> Update(Guid id, LocationSaveDto dto, CancellationToken ct)
        => Success(await service.UpdateAsync(id, dto, ct), "儲位已更新。");

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.LocationManage)]
    public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Success("儲位已刪除。");
    }
}
