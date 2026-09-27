using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Services;

namespace Wms.Api.Controllers;

/// <summary>出庫單：建立、分配、出貨。</summary>
[Route("api/v1/outbound-orders")]
public class OutboundOrdersController(IOutboundService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.OutboundView)]
    public async Task<ApiResponse<PagedResult<OutboundOrderDto>>> Query([FromQuery] OutboundQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.OutboundView)]
    public async Task<ApiResponse<OutboundOrderDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.OutboundCreate)]
    public async Task<ApiResponse<OutboundOrderDto>> Create(OutboundOrderCreateDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "出庫單已建立。");

    /// <summary>庫存分配：預留庫存並產生揀貨任務。</summary>
    [HttpPost("{id:guid}/allocate")]
    [Authorize(Policy = Permissions.OutboundAllocate)]
    public async Task<ApiResponse<AllocationResultDto>> Allocate(Guid id, CancellationToken ct)
    {
        var result = await service.AllocateAsync(id, ct);
        var message = result.FullyAllocated
            ? "庫存分配完成，已產生揀貨任務。"
            : "部分明細庫存不足，僅完成部分分配。";

        return Success(result, message);
    }

    /// <summary>取消分配：釋放尚未揀出的預留量。</summary>
    [HttpPost("{id:guid}/release")]
    [Authorize(Policy = Permissions.OutboundAllocate)]
    public async Task<ApiResponse<OutboundOrderDto>> Release(Guid id, CancellationToken ct)
        => Success(await service.ReleaseAsync(id, ct), "已取消分配並釋放預留庫存。");

    /// <summary>出貨：所有揀貨任務完成後才可執行。</summary>
    [HttpPost("{id:guid}/ship")]
    [Authorize(Policy = Permissions.OutboundShip)]
    public async Task<ApiResponse<OutboundOrderDto>> Ship(Guid id, ShipRequest request, CancellationToken ct)
        => Success(await service.ShipAsync(id, request, ct), "出貨完成。");

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.OutboundCreate)]
    public async Task<ApiResponse<OutboundOrderDto>> Cancel(Guid id, CancellationToken ct)
        => Success(await service.CancelAsync(id, ct), "出庫單已取消。");
}

/// <summary>揀貨任務。</summary>
[Route("api/v1/pick-tasks")]
public class PickTasksController(IPickingService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.PickingView)]
    public async Task<ApiResponse<PagedResult<PickTaskDto>>> Query([FromQuery] PickQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PickingView)]
    public async Task<ApiResponse<PickTaskDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public async Task<ApiResponse<PickTaskDto>> Assign(Guid id, AssignTaskRequest request, CancellationToken ct)
        => Success(await service.AssignAsync(id, request.UserId, ct), "任務已指派。");

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public async Task<ApiResponse<PickTaskDto>> Start(Guid id, CancellationToken ct)
        => Success(await service.StartAsync(id, ct), "任務已開始。");

    /// <summary>完成揀貨：從來源儲位扣帳並消耗預留量。</summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public async Task<ApiResponse<PickTaskDto>> Complete(Guid id, PickCompleteRequest request, CancellationToken ct)
        => Success(await service.CompleteAsync(id, request, ct), "揀貨完成，庫存已扣除。");

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public async Task<ApiResponse<PickTaskDto>> Cancel(Guid id, CancellationToken ct)
        => Success(await service.CancelAsync(id, ct), "任務已取消。");
}
