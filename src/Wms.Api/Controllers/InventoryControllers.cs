using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Services;

namespace Wms.Api.Controllers;

/// <summary>即時庫存。</summary>
public class InventoryController(IInventoryService service) : ApiControllerBase
{
    /// <summary>依儲位列出庫存明細。</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.InventoryView)]
    public async Task<ApiResponse<PagedResult<InventoryDto>>> Query([FromQuery] InventoryQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    /// <summary>依物料彙總庫存。</summary>
    [HttpGet("summary")]
    [Authorize(Policy = Permissions.InventoryView)]
    public async Task<ApiResponse<PagedResult<InventorySummaryDto>>> Summary([FromQuery] InventoryQuery query, CancellationToken ct)
        => Success(await service.SummaryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.InventoryView)]
    public async Task<ApiResponse<InventoryDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    /// <summary>人工調整庫存，會產生 ADJUSTMENT 異動。</summary>
    [HttpPost("adjust")]
    [Authorize(Policy = Permissions.InventoryAdjust)]
    public async Task<ApiResponse<InventoryDto>> Adjust(InventoryAdjustDto dto, CancellationToken ct)
        => Success(await service.AdjustAsync(dto, ct), "庫存已調整。");
}

/// <summary>庫存異動流水，唯讀。</summary>
[Route("api/v1/inventory-transactions")]
public class InventoryTransactionsController(IInventoryService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.InventoryView)]
    public async Task<ApiResponse<PagedResult<InventoryTransactionDto>>> Query([FromQuery] TransactionQuery query, CancellationToken ct)
        => Success(await service.QueryTransactionsAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.InventoryView)]
    public async Task<ApiResponse<InventoryTransactionDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetTransactionAsync(id, ct));
}

/// <summary>移庫。</summary>
[Route("api/v1/transfers")]
public class TransfersController(ITransferService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.TransferView)]
    public async Task<ApiResponse<PagedResult<TransferOrderDto>>> Query([FromQuery] TransferQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.TransferView)]
    public async Task<ApiResponse<TransferOrderDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.TransferExecute)]
    public async Task<ApiResponse<TransferOrderDto>> Create(TransferCreateDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct),
            dto.ExecuteImmediately ? "移庫已完成。" : "移庫單已建立。");

    [HttpPost("{id:guid}/execute")]
    [Authorize(Policy = Permissions.TransferExecute)]
    public async Task<ApiResponse<TransferOrderDto>> Execute(Guid id, CancellationToken ct)
        => Success(await service.ExecuteAsync(id, ct), "移庫已完成，庫存已轉移。");

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.TransferExecute)]
    public async Task<ApiResponse<TransferOrderDto>> Cancel(Guid id, CancellationToken ct)
        => Success(await service.CancelAsync(id, ct), "移庫單已取消。");
}

/// <summary>盤點。</summary>
[Route("api/v1/stocktakes")]
public class StocktakesController(IStocktakeService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.StocktakeView)]
    public async Task<ApiResponse<PagedResult<StocktakeDto>>> Query([FromQuery] StocktakeQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.StocktakeView)]
    public async Task<ApiResponse<StocktakeDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    /// <summary>建立盤點單，同時 Snapshot 目前系統庫存。</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.StocktakeCreate)]
    public async Task<ApiResponse<StocktakeDto>> Create(StocktakeCreateDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "盤點單已建立。");

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Permissions.StocktakeCount)]
    public async Task<ApiResponse<StocktakeDto>> Start(Guid id, CancellationToken ct)
        => Success(await service.StartAsync(id, ct), "盤點已開始。");

    /// <summary>輸入盤點數量，系統自動計算差異。</summary>
    [HttpPost("{id:guid}/count")]
    [Authorize(Policy = Permissions.StocktakeCount)]
    public async Task<ApiResponse<StocktakeDto>> Count(Guid id, StocktakeCountRequest request, CancellationToken ct)
        => Success(await service.CountAsync(id, request, ct), "盤點數已記錄。");

    /// <summary>指定明細複盤。</summary>
    [HttpPost("{id:guid}/recount")]
    [Authorize(Policy = Permissions.StocktakeCount)]
    public async Task<ApiResponse<StocktakeDto>> Recount(Guid id, StocktakeRecountRequest request, CancellationToken ct)
        => Success(await service.RecountAsync(id, request, ct), "已轉為複盤。");

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Permissions.StocktakeCount)]
    public async Task<ApiResponse<StocktakeDto>> Complete(Guid id, CancellationToken ct)
        => Success(await service.CompleteAsync(id, ct), "盤點結束，等待核准。");

    /// <summary>核准盤點並依差異調整庫存。</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Permissions.StocktakeApprove)]
    public async Task<ApiResponse<StocktakeDto>> Approve(Guid id, CancellationToken ct)
        => Success(await service.ApproveAsync(id, ct), "盤點已核准，庫存差異已調整。");

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.StocktakeCreate)]
    public async Task<ApiResponse<StocktakeDto>> Cancel(Guid id, CancellationToken ct)
        => Success(await service.CancelAsync(id, ct), "盤點單已取消。");
}
