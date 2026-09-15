using System.Text.Json;
using MongoDB.Driver;
using Persistence.Abstractions;
using Shared.Models;

namespace Persistence.MongoDB;

public class MongoDbRepository : IStatisticsRepository
{
    private readonly IMongoCollection<ServerStatistics> _collection;
    public MongoDbRepository()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("ServerMonitoringDb");
        _collection = database.GetCollection<ServerStatistics>("ServerStatistics");
    }

    public async Task SaveAsync(ServerStatistics data)
    {
        Console.WriteLine($"{JsonSerializer.Serialize(data)} Added to mongodb successfully");
    }
}