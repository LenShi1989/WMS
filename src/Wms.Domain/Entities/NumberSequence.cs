namespace Wms.Domain.Entities;

/// <summary>單號流水序列。以 key（例如 IB-20260927）為單位原子遞增。</summary>
public class NumberSequence
{
    public string Key { get; set; } = string.Empty;
    public long CurrentValue { get; set; }
}
