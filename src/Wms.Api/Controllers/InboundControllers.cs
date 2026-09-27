using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Services;

namespace Wms.Api.Controllers;

/// <summary>入庫單：建立、收貨、結案。</summary>
[Route("api/v1/inbound-orders")]
public class InboundOrdersController(IInboundService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.InboundView)]
    public async Task<ApiResponse<PagedResult<InboundOrderDto>>> Query([FromQuery] InboundQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.InboundView)]
    public async Task<ApiResponse<InboundOrderDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.InboundCreate)]
    public async Task<ApiResponse<InboundOrderDto>> Create(InboundOrderCreateDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "入庫單已建立。");

    /// <summary>收貨：庫存進入收貨區，並自動產生上架任務。</summary>
    [HttpPost("{id:guid}/receive")]
    [Authorize(Policy = Permissions.InboundReceive)]
    public async Task<ApiResponse<InboundOrderDto>> Receive(Guid id, ReceiveRequest request, CancellationToken ct)
        => Success(await service.ReceiveAsync(id, request, ct), "收貨完成，已產生上架任務。");

    /// <summary>結案：所有上架任務完成後才可執行。</summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Permissions.InboundComplete)]
    public async Task<ApiResponse<InboundOrderDto>> Complete(Guid id, CancellationToken ct)
        => Success(await service.CompleteAsync(id, ct), "入庫單已結案。");

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.InboundCreate)]
    public async Task<ApiResponse<InboundOrderDto>> Cancel(Guid id, CancellationToken ct)
        => Success(await service.CancelAsync(id, ct), "入庫單已取消。");
}

/// <summary>上架任務。</summary>
[Route("api/v1/putaway-tasks")]
public class PutawayTasksController(IPutawayService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.PutawayView)]
    public async Task<ApiResponse<PagedResult<PutawayTaskDto>>> Query([FromQuery] PutawayQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PutawayView)]
    public async Task<ApiResponse<PutawayTaskDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = Permissions.PutawayExecute)]
    public async Task<ApiResponse<PutawayTaskDto>> Assign(Guid id, AssignTaskRequest request, CancellationToken ct)
        => Success(await service.AssignAsync(id, request.UserId, ct), "任務已指派。");

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Permissions.PutawayExecute)]
    public async Task<ApiResponse<PutawayTaskDto>> Start(Guid id, CancellationToken ct)
        => Success(await service.StartAsync(id, ct), "任務已開始。");

    /// <summary>完成上架：庫存由收貨區移入目標儲位。</summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Permissions.PutawayExecute)]
    public async Task<ApiResponse<PutawayTaskDto>> Complete(Guid id, PutawayCompleteRequest request, CancellationToken ct)
        => Success(await service.CompleteAsync(id, request, ct), "上架完成，庫存已更新。");

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.PutawayExecute)]
    public async Task<ApiResponse<PutawayTaskDto>> Cancel(Guid id, CancellationToken ct)
        => Success(await service.CancelAsync(id, ct), "任務已取消。");
}
