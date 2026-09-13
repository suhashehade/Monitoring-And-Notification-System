using Shared.Models;

namespace Messaging;

public class RabbitMq: IMessagePublisher
{
    public Task PublishAsync(string topic, ServerStatistics data)
    {
        Console.WriteLine($"Publishing to {topic} from {Environment.MachineName} from RabbitMQ");
      Console.WriteLine($"[{topic}] Stats collected at {data?.Timestamp}: CPU: {Math.Round(data.CpuUsage, 2)}%, Mem Usage: {Math.Round(data.MemoryUsage, 2)}MB");
        return Task.CompletedTask;
    }
}