using Shared.Models;

namespace Messaging;

public interface IMessagePublisher
{
    Task PublishAsync(string topic, ServerStatistics data);
}