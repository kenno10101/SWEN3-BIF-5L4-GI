using SWEN_DMS.Models;

namespace SWEN_DMS.DAL.Repositories;

public interface IAccessLogDailyRepository
{
    Task UpsertIncrementAsync(Guid documentId, DateOnly dayUtc, int increment, CancellationToken ct = default);
}