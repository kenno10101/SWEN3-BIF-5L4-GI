using Microsoft.EntityFrameworkCore;
using SWEN_DMS.Models;

namespace SWEN_DMS.DAL.Repositories;

public class AccessLogDailyRepository : IAccessLogDailyRepository
{
    private readonly AppDbContext _ctx;

    public AccessLogDailyRepository(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task UpsertIncrementAsync(Guid documentId, DateOnly dayUtc, int increment, CancellationToken ct = default)
    {
        if (increment <= 0) return;

        var existing = await _ctx.AccessLogsDaily
            .FirstOrDefaultAsync(x => x.DocumentId == documentId && x.DayUtc == dayUtc, ct);

        if (existing is null)
        {
            _ctx.AccessLogsDaily.Add(new AccessLogDaily
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                DayUtc = dayUtc,
                AccessCount = increment,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.AccessCount += increment;
        }

        await _ctx.SaveChangesAsync(ct);
    }
}