namespace Wms.Domain.Enums;

/// <summary>儲區類型。</summary>
public enum ZoneType
{
    RECEIVING,
    STORAGE,
    PICKING,
    STAGING,
    SHIPPING,
    QC,
    NG
}

/// <summary>儲位類型。</summary>
public enum LocationType
{
    SHELF,
    RACK,
    PALLET,
    FLOOR,
    BIN,
    STAGING,
    VIRTUAL
}

/// <summary>入庫單來源。</summary>
public enum InboundSourceType
{
    MANUAL,
    ERP,
    PURCHASE,
    RETURN,
    PRODUCTION
}

/// <summary>入庫單狀態。</summary>
public enum InboundStatus
{
    DRAFT,
    RECEIVING,
    RECEIVED,
    PUTAWAY,
    COMPLETED,
    CANCELLED
}

/// <summary>入庫明細狀態。</summary>
public enum InboundDetailStatus
{
    PENDING,
    PARTIAL,
    RECEIVED,
    PUTAWAY,
    COMPLETED,
    CANCELLED
}

/// <summary>倉儲任務狀態（上架 / 揀貨共用）。</summary>
public enum WmsTaskStatus
{
    PENDING,
    ASSIGNED,
    PROCESSING,
    COMPLETED,
    CANCELLED
}

/// <summary>出庫單狀態。</summary>
public enum OutboundStatus
{
    DRAFT,
    ALLOCATED,
    PICKING,
    PICKED,
    SHIPPING,
    SHIPPED,
    CANCELLED
}

/// <summary>出庫明細狀態。</summary>
public enum OutboundDetailStatus
{
    PENDING,
    PARTIAL_ALLOCATED,
    ALLOCATED,
    PICKING,
    PICKED,
    SHIPPED,
    CANCELLED
}

/// <summary>庫存狀態。</summary>
public enum InventoryStatus
{
    NORMAL,
    HOLD,
    QC,
    NG,
    FROZEN
}

/// <summary>庫存異動類型。</summary>
public enum TransactionType
{
    RECEIVE,
    PUTAWAY,
    PICK,
    SHIP,
    TRANSFER,
    ADJUSTMENT,
    STOCKTAKE,
    SCRAP,
    RESERVE,
    RELEASE
}

/// <summary>移庫單狀態。</summary>
public enum TransferStatus
{
    DRAFT,
    PENDING,
    COMPLETED,
    CANCELLED
}

/// <summary>盤點單狀態。</summary>
public enum StocktakeStatus
{
    DRAFT,
    COUNTING,
    COUNTED,
    RECOUNT,
    PENDING_APPROVAL,
    APPROVED,
    CANCELLED
}

/// <summary>盤點明細狀態。</summary>
public enum StocktakeDetailStatus
{
    PENDING,
    COUNTED,
    DIFFERENCE,
    RECOUNT,
    ADJUSTED
}

/// <summary>異動來源單據類型。</summary>
public enum ReferenceType
{
    INBOUND_ORDER,
    PUTAWAY_TASK,
    OUTBOUND_ORDER,
    PICK_TASK,
    TRANSFER_ORDER,
    STOCKTAKE,
    MANUAL
}
