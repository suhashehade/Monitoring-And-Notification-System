using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Models;

namespace Messaging.RabbitMQ;

public class RabbitMqConsumer : IMessageConsumer
{
    private const string ExchangeName = "server_statistics_exchange";
    private readonly IChannel _channel;

    private readonly IConnection _connection;

    public RabbitMqConsumer()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            ExchangeName,
            ExchangeType.Topic
        ).GetAwaiter().GetResult();
    }

    public void Subscribe(string topicPattern, Action<ServerStatistics> onMessageReceived)
    {
        var queueName = _channel.QueueDeclareAsync(
            "",
            false,
            true,
            true
        ).GetAwaiter().GetResult().QueueName;

        _channel.QueueBindAsync(
            queueName,
            ExchangeName,
            topicPattern
        ).GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.Span);
            var data = JsonSerializer.Deserialize<ServerStatistics>(json);

            if (data is not null) onMessageReceived(data);

            await Task.CompletedTask;
        };

        _channel.BasicConsumeAsync(
            queueName,
            true,
            consumer
        ).GetAwaiter().GetResult();
    }
}