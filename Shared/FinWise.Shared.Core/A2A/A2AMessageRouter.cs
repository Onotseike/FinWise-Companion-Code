using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinWise.Shared.Core.A2A;

/// <summary>
/// Utility for sending and receiving inter-agent messages via Azure Service Bus.
/// Handles serialization, retry logic, tracking, and error management.
/// </summary>
/// <remarks>
/// Initializes a new instance of the A2AMessageRouter class.
/// </remarks>
/// <param name="serviceBusClient">The Service Bus client instance</param>
/// <param name="topicName">The name of the Service Bus topic for messaging</param>
/// <param name="subscriptionName">The subscription name for this agent</param>
/// <param name="messageTimeoutMs">Timeout for waiting for responses (default 30s)</param>
/// <param name="maxRetries">Maximum retries for transient failures (default 3)</param>
/// <param name="_logger">_logger instance</param>
public class A2AMessageRouter(
    ServiceBusClient serviceBusClient,
    string topicName,
    string subscriptionName,
    ILogger<A2AMessageRouter> _logger,
    int messageTimeoutMs = 30000,
    int maxRetries = 3)
{
    private readonly ServiceBusClient _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
    private readonly string _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
    private readonly string _subscriptionName = subscriptionName ?? throw new ArgumentNullException(nameof(subscriptionName));
    private readonly int _messageTimeoutMs = messageTimeoutMs;
    private readonly int _maxRetries = maxRetries;
    private readonly ILogger<A2AMessageRouter> __logger = _logger ?? throw new ArgumentNullException(nameof(_logger));

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Sends an agent message to a recipient agent via Service Bus topic.
    /// </summary>
    /// <param name="envelope">The message envelope to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="A2AMessagingException">Thrown if the send operation fails</exception>
    public async Task SendMessageAsync<TPayload>(
        AgentEnvelope<TPayload> envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        try
        {
            var sender = _serviceBusClient.CreateSender(_topicName);
            await using (sender.ConfigureAwait(false))
            {
                var json = JsonSerializer.Serialize(envelope, SerializerOptions);
                var sbMessage = new ServiceBusMessage(json)
                {
                    CorrelationId = envelope.CorrelationId,
                    SessionId = envelope.CorrelationId, // Enable session-based correlation
                    Subject = envelope.MessageType,
                    TimeToLive = TimeSpan.FromMilliseconds(envelope.TimeoutMs),
                    ApplicationProperties =
                    {
                        { "sender", envelope.Sender },
                        { "recipient", envelope.Recipient },
                        { "messageType", envelope.MessageType },
                        { "traceId", envelope.TraceId ?? envelope.CorrelationId }
                    }
                };

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Sending A2A message: CorrelationId={CorrelationId}, Type={MessageType}, From={Sender} To={Recipient}",
                        envelope.CorrelationId, envelope.MessageType, envelope.Sender, envelope.Recipient);
                }

                await sender.SendMessageAsync(sbMessage, cancellationToken);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "A2A message sent successfully: CorrelationId={CorrelationId}, Type={MessageType}",
                        envelope.CorrelationId, envelope.MessageType);
                }
            }
        }
        catch (ServiceBusException ex) when (ex.IsTransient)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    ex,
                    "Transient error sending A2A message {CorrelationId}: {Error}",
                    envelope.CorrelationId, ex.Message);
            }
            throw new A2AMessagingException(
                $"Transient error sending message {envelope.CorrelationId}", ex, isRetryable: true);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    ex,
                    "Failed to send A2A message {CorrelationId}: {Error}",
                    envelope.CorrelationId, ex.Message);
            }
            throw new A2AMessagingException(
                $"Failed to send A2A message {envelope.CorrelationId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Receives and deserializes the next agent message from the subscription.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized agent envelope, or null if no message is available</returns>
    /// <exception cref="A2AMessagingException">Thrown if the receive operation fails</exception>
    public async Task<AgentEnvelope<object>?> ReceiveMessageAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var receiver = _serviceBusClient.CreateReceiver(
                _topicName,
                _subscriptionName,
                new ServiceBusReceiverOptions
                {
                    ReceiveMode = ServiceBusReceiveMode.PeekLock
                });

            await using (receiver.ConfigureAwait(false))
            {
                var sbMessage = await receiver.ReceiveMessageAsync(
                    TimeSpan.FromMilliseconds(_messageTimeoutMs), cancellationToken);

                if (sbMessage == null)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(
                            "No message received from subscription {SubscriptionName} within timeout",
                            _subscriptionName);
                    }
                    return null;
                }

                var json = sbMessage.Body.ToString();
                var envelope = JsonSerializer.Deserialize<AgentEnvelope<object>>(json, SerializerOptions)
                    ?? throw new InvalidOperationException("Failed to deserialize message");

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "A2A message received: CorrelationId={CorrelationId}, Type={MessageType}",
                        envelope.CorrelationId, envelope.MessageType);
                }

                // Complete the message (remove from queue)
                await receiver.CompleteMessageAsync(sbMessage, cancellationToken);

                return envelope;
            }
        }
        catch (ServiceBusException ex) when (ex.IsTransient)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    ex,
                    "Transient error receiving A2A message: {Error}",
                    ex.Message);
            }
            throw new A2AMessagingException(
                $"Transient error receiving message", ex, isRetryable: true);
        }
        catch (OperationCanceledException ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    ex,
                    "Receive operation cancelled");
            }
            throw new A2AMessagingException(
                "Receive operation cancelled", ex, isRetryable: false);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    ex,
                    "Failed to receive A2A message: {Error}",
                    ex.Message);
            }
            throw new A2AMessagingException(
                $"Failed to receive A2A message: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes a JSON payload to the specified type.
    /// </summary>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized payload</returns>
    public static TPayload DeserializePayload<TPayload>(string json) => JsonSerializer.Deserialize<TPayload>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize payload to type {typeof(TPayload).Name}");

    /// <summary>
    /// Serializes a payload to JSON.
    /// </summary>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="payload">The payload to serialize</param>
    /// <returns>The JSON string representation</returns>
    public static string SerializePayload<TPayload>(TPayload payload) => JsonSerializer.Serialize(payload, SerializerOptions);
}

/// <summary>
/// Exception thrown when A2A messaging operations fail.
/// </summary>
/// <remarks>
/// Initializes a new instance of the A2AMessagingException class.
/// </remarks>
public class A2AMessagingException(
    string message,
    Exception? innerException = null,
    bool isRetryable = false) : Exception(message, innerException)
{
    /// <summary>
    /// Gets a value indicating whether the error is retryable.
    /// </summary>
    public bool IsRetryable { get; } = isRetryable;
}
