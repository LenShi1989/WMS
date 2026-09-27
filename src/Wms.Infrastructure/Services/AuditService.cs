using System.Text.Json;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;
using Wms.Infrastructure.Persistence;

namespace Wms.Infrastructure.Services;

public class AuditService(WmsDbContext db, ICurrentUser currentUser) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task LogAsync(
        string action,
        string module,
        string? referenceType = null,
        Guid? referenceId = null,
        string? referenceNo = null,
        object? oldValue = null,
        object? newValue = null,
        CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = currentUser.UserId,
            Username = currentUser.Username,
            Action = action,
            Module = module,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceNo = referenceNo,
            OldValue = Serialize(oldValue),
            NewValue = Serialize(newValue),
            IpAddress = currentUser.IpAddress
        });

        await db.SaveChangesAsync(ct);
    }

    private static string? Serialize(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return value is string text
            ? JsonSerializer.Serialize(text, JsonOptions)
            : JsonSerializer.Serialize(value, JsonOptions);
    }
}
