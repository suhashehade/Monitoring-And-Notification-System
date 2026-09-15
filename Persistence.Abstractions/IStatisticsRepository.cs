using Shared.Models;

namespace Persistence.Abstractions;

public interface IStatisticsRepository
{
    Task SaveAsync(ServerStatistics data);
}