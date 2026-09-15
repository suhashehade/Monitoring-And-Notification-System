using Shared.Models;

namespace Messaging;

public interface IMessageConsumer
{
    void Subscribe(string topicPattern, Action<ServerStatistics> onMessageReceived);
}