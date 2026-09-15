using Messaging;

namespace AnomalyDetectionService.Handlers;

public class ConsumeHandler
{
    private readonly IMessageConsumer _messageConsumer;

    public ConsumeHandler(IMessageConsumer messageConsumer)
    {
        _messageConsumer = messageConsumer;
    }
    
    public void Consumer()
    {
        _messageConsumer.Subscribe("ServerStatistics.*",
            stats => { Console.WriteLine($"Received from {stats.ServerIdentifier}: CPU {stats.CpuUsage}%"); });
    }
}