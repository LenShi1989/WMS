using Microsoft.EntityFrameworkCore;
using Wms.Application.Interfaces;
using Wms.Infrastructure.Persistence;

namespace Wms.Infrastructure.Services;

/// <summary>
/// 單號產生器，格式為 {前綴}{yyyyMMdd}{4 位流水}，例如 IB202609270001。
/// 以 UPSERT ... RETURNING 取號，確保並行下不會撞號。
/// </summary>
public class NumberGenerator(WmsDbContext db) : INumberGenerator
{
    public async Task<string> NextAsync(string prefix, CancellationToken ct = default)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var key = $"{prefix}-{date}";

        var next = await db.Database
            .SqlQueryRaw<long>(
                """
                INSERT INTO number_sequences (key, current_value)
                VALUES ({0}, 1)
                ON CONFLICT (key) DO UPDATE SET current_value = number_sequences.current_value + 1
                RETURNING current_value AS "Value"
                """,
                key)
            .ToListAsync(ct);

        var sequence = next.FirstOrDefault();

        return $"{prefix}{date}{sequence:D4}";
    }
}
