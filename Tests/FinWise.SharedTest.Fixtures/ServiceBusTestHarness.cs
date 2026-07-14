using Azure.Messaging.ServiceBus;
using FinWise.Shared.Core.A2A;
using Moq;
using System.Collections.Concurrent;

namespace FinWise.SharedTest.Fixtures;

/// <summary>
/// In-memory test harness for Azure Service Bus.
/// Simulates Service Bus topic/subscription behavior for testing inter-agent communication.
/// </summary>
public class ServiceBusTestHarness : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Queue<ServiceBusMessage>> _topics = new();
    private readonly Mock<ServiceBusClient> _mockClient;

    public ServiceBusTestHarness() => _mockClient = new Mock<ServiceBusClient>();

    /// <summary>
    /// Gets the mock Service Bus client for dependency injection.
    /// </summary>
    public Mock<ServiceBusClient> MockClient => _mockClient;

    /// <summary>
    /// Gets or creates a topic for testing.
    /// </summary>
    /// <param name="topicName">The name of the topic</param>
    /// <returns>The topic's message queue</returns>
    public Queue<ServiceBusMessage> GetOrCreateTopic(string topicName) => _topics.GetOrAdd(topicName, _ => new Queue<ServiceBusMessage>());

    /// <summary>
    /// Enqueues a message to a topic (simulating a send operation).
    /// </summary>
    /// <param name="topicName">The target topic name</param>
    /// <param name="message">The message to enqueue</param>
    public void EnqueueMessage(string topicName, ServiceBusMessage message)
    {
        var topic = GetOrCreateTopic(topicName);
        lock (topic)
        {
            topic.Enqueue(message);
        }
    }

    /// <summary>
    /// Dequeues a message from a topic (simulating a receive operation).
    /// </summary>
    /// <param name="topicName">The source topic name</param>
    /// <returns>The next message, or null if the queue is empty</returns>
    public ServiceBusMessage? DequeueMessage(string topicName)
    {
        if (_topics.TryGetValue(topicName, out var topic))
        {
            lock (topic)
            {
                return topic.Count > 0 ? topic.Dequeue() : null;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the number of messages in a topic.
    /// </summary>
    /// <param name="topicName">The topic name</param>
    /// <returns>The message count</returns>
    public int GetMessageCount(string topicName) => _topics.TryGetValue(topicName, out var topic) ? topic.Count : 0;

    /// <summary>
    /// Clears all messages from a topic.
    /// </summary>
    /// <param name="topicName">The topic name</param>
    public void ClearTopic(string topicName)
    {
        if (_topics.TryGetValue(topicName, out var topic))
        {
            lock (topic)
            {
                topic.Clear();
            }
        }
    }

    /// <summary>
    /// Clears all topics.
    /// </summary>
    public void ClearAllTopics() => _topics.Clear();

    /// <summary>
    /// Simulates sending an agent envelope message.
    /// </summary>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="topicName">The target topic</param>
    /// <param name="envelope">The agent envelope to send</param>
    public void SendEnvelopeMessage<TPayload>(string topicName, AgentEnvelope<TPayload> envelope)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(envelope);
        var sbMessage = new ServiceBusMessage(json)
        {
            CorrelationId = envelope.CorrelationId,
            SessionId = envelope.CorrelationId,
            Subject = envelope.MessageType
        };

        EnqueueMessage(topicName, sbMessage);
    }

    /// <summary>
    /// Retrieves the next envelope message from a topic.
    /// </summary>
    /// <param name="topicName">The source topic</param>
    /// <returns>The deserialized envelope, or null if queue is empty</returns>
    public AgentEnvelope<object>? ReceiveEnvelopeMessage(string topicName)
    {
        var sbMessage = DequeueMessage(topicName);
        if (sbMessage == null)
            return null;

        var json = sbMessage.Body.ToString();
        return System.Text.Json.JsonSerializer.Deserialize<AgentEnvelope<object>>(json);
    }

    /// <summary>
    /// Disposes the test harness.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        ClearAllTopics();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Helper for creating test Service Bus messages.
/// </summary>
public static class ServiceBusMessageBuilder
{
    /// <summary>
    /// Creates a test Service Bus message with the specified body.
    /// </summary>
    /// <param name="body">The message body</param>
    /// <returns>A ServiceBusMessage instance</returns>
    public static ServiceBusMessage CreateTestMessage(string body) => new(body);

    /// <summary>
    /// Creates a test Service Bus message from a JSON object.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize</typeparam>
    /// <param name="obj">The object to serialize</param>
    /// <returns>A ServiceBusMessage instance</returns>
    public static ServiceBusMessage CreateTestMessageFromJson<T>(T obj)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj);
        return new ServiceBusMessage(json);
    }
}
