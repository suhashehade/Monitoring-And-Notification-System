using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Models;

namespace Messaging.RabbitMQ;

public class RabbitMqPublisher: IMessagePublisher
{
    private const string ExchangeName = "server_statistics_exchange";
    
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    
    public RabbitMqPublisher()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic
        ).GetAwaiter().GetResult();
    }
    public async Task PublishAsync(string topic, ServerStatistics data)
    {
        Console.WriteLine($"{JsonSerializer.Serialize(data)} from RabbitMq");
        var json = JsonSerializer.Serialize(data);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: topic,
            body: body
        );
    }
}