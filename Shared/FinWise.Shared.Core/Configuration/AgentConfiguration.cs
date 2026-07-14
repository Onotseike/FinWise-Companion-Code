using System.Text.Json.Serialization;

namespace FinWise.Shared.Core.Configuration;

/// <summary>
/// Unified configuration for all FinWise agents.
/// Standardizes and validates agent settings including Azure OpenAI, Service Bus, and logging.
/// </summary>
public class AgentConfiguration
{
    /// <summary>
    /// Gets or sets the Azure OpenAI configuration section.
    /// </summary>
    [JsonPropertyName("azureOpenAI")]
    public AzureOpenAISettings AzureOpenAI { get; set; } = new();

    /// <summary>
    /// Gets or sets the agent identity and management settings.
    /// </summary>
    [JsonPropertyName("agent")]
    public AgentIdentitySettings Agent { get; set; } = new();

    /// <summary>
    /// Gets or sets the Service Bus configuration for inter-agent communication.
    /// </summary>
    [JsonPropertyName("serviceBus")]
    public ServiceBusSettings ServiceBus { get; set; } = new();

    /// <summary>
    /// Gets or sets the external API integrations (Toshl, EstateIntel, etc.)
    /// </summary>
    [JsonPropertyName("externalApis")]
    public ExternalApiSettings ExternalApis { get; set; } = new();

    /// <summary>
    /// Gets or sets the state persistence settings (Cosmos DB, etc.)
    /// </summary>
    [JsonPropertyName("persistence")]
    public PersistenceSettings Persistence { get; set; } = new();

    /// <summary>
    /// Validates the configuration and throws if required settings are missing.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if required configuration is missing</exception>
    public void Validate()
    {
        AzureOpenAI.Validate();
        Agent.Validate();
        ServiceBus.Validate();
        // Persistence is optional for Phase 1
    }
}

/// <summary>
/// Azure OpenAI configuration settings.
/// </summary>
public class AzureOpenAISettings
{
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("deploymentName")]
    public string? DeploymentName { get; set; }

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; } = "2024-10-21";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new InvalidOperationException("AzureOpenAI.Endpoint is required");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("AzureOpenAI.ApiKey is required");
        }

        if (string.IsNullOrWhiteSpace(DeploymentName))
        {
            throw new InvalidOperationException("AzureOpenAI.DeploymentName is required");
        }
    }
}

/// <summary>
/// Agent identity and management settings.
/// </summary>
public class AgentIdentitySettings
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("instructions")]
    public string? Instructions { get; set; }

    [JsonPropertyName("managementEndpoint")]
    public string ManagementEndpoint { get; set; } = "http://localhost:7071";

    [JsonPropertyName("heartbeatFrequencySeconds")]
    public int HeartbeatFrequencySeconds { get; set; } = 300; // 5 minutes

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("Agent.Id is required");
        }

        if (string.IsNullOrWhiteSpace(ManagementEndpoint))
        {
            throw new InvalidOperationException("Agent.ManagementEndpoint is required");
        }

        if (!Uri.TryCreate(ManagementEndpoint, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"Agent.ManagementEndpoint '{ManagementEndpoint}' is not a valid URI");
        }

        if (HeartbeatFrequencySeconds <= 0)
        {
            throw new InvalidOperationException("Agent.HeartbeatFrequencySeconds must be greater than zero");
        }
    }
}

/// <summary>
/// Azure Service Bus configuration for inter-agent A2A communication.
/// </summary>
public class ServiceBusSettings
{
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    [JsonPropertyName("topicName")]
    public string TopicName { get; set; } = "agent-messages";

    [JsonPropertyName("subscriptionName")]
    public string? SubscriptionName { get; set; }

    [JsonPropertyName("maxConcurrentCalls")]
    public int MaxConcurrentCalls { get; set; } = 10;

    [JsonPropertyName("maxAutoLockRenewalDuration")]
    public int MaxAutoLockRenewalDurationSeconds { get; set; } = 300;

    [JsonPropertyName("messageTimeoutSeconds")]
    public int MessageTimeoutSeconds { get; set; } = 30;

    [JsonPropertyName("deadLetterPolicy")]
    public DeadLetterPolicy DeadLetterPolicy { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("ServiceBus.ConnectionString is required");
        }

        if (string.IsNullOrWhiteSpace(SubscriptionName))
        {
            throw new InvalidOperationException("ServiceBus.SubscriptionName is required");
        }
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(ConnectionString);
}

/// <summary>
/// Dead letter policy for failed messages in Service Bus.
/// </summary>
public class DeadLetterPolicy
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("maxDeliveryCount")]
    public int MaxDeliveryCount { get; set; } = 3;
}

/// <summary>
/// External API integrations configuration.
/// </summary>
public class ExternalApiSettings
{
    [JsonPropertyName("toshl")]
    public ToshlApiSettings Toshl { get; set; } = new();

    [JsonPropertyName("estateIntel")]
    public EstateIntelApiSettings EstateIntel { get; set; } = new();

    [JsonPropertyName("retryPolicy")]
    public RetryPolicySettings RetryPolicy { get; set; } = new();
}

/// <summary>
/// Toshl API configuration.
/// </summary>
public class ToshlApiSettings
{
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "https://api.toshl.com";

    [JsonPropertyName("apiToken")]
    public string? ApiToken { get; set; }

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// EstateIntel (real estate) API configuration.
/// </summary>
public class EstateIntelApiSettings
{
    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;
}

/// <summary>
/// Retry policy for external API calls.
/// </summary>
public class RetryPolicySettings
{
    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    [JsonPropertyName("initialBackoffSeconds")]
    public int InitialBackoffSeconds { get; set; } = 1;

    [JsonPropertyName("maxBackoffSeconds")]
    public int MaxBackoffSeconds { get; set; } = 30;

    [JsonPropertyName("backoffMultiplier")]
    public double BackoffMultiplier { get; set; } = 2.0;
}

/// <summary>
/// State persistence configuration (Cosmos DB, SQL, etc.)
/// </summary>
public class PersistenceSettings
{
    [JsonPropertyName("type")]
    public PersistenceType Type { get; set; } = PersistenceType.None;

    [JsonPropertyName("cosmosDb")]
    public CosmosDbSettings CosmosDb { get; set; } = new();

    [JsonPropertyName("sqlDatabase")]
    public SqlDatabaseSettings SqlDatabase { get; set; } = new();
}

/// <summary>
/// Cosmos DB configuration for state persistence.
/// </summary>
public class CosmosDbSettings
{
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("authKey")]
    public string? AuthKey { get; set; }

    [JsonPropertyName("databaseName")]
    public string DatabaseName { get; set; } = "FinWise";

    [JsonPropertyName("conversationsContainerName")]
    public string ConversationsContainerName { get; set; } = "Conversations";

    [JsonPropertyName("agentStateContainerName")]
    public string AgentStateContainerName { get; set; } = "AgentState";

    [JsonPropertyName("throughputRus")]
    public int ThroughputRus { get; set; } = 400;
}

/// <summary>
/// SQL Database configuration for state persistence.
/// </summary>
public class SqlDatabaseSettings
{
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    [JsonPropertyName("commandTimeoutSeconds")]
    public int CommandTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Persistence provider type.
/// </summary>
public enum PersistenceType
{
    None = 0,
    CosmosDb = 1,
    SqlDatabase = 2,
    BlobStorage = 3
}
