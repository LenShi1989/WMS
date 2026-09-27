namespace Wms.Domain.Common;

/// <summary>所有 Entity 的共同基底：UUID 主鍵 + 建立時間。</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>需要追蹤更新時間的 Entity。</summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
