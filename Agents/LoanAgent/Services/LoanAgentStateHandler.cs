using Microsoft.Extensions.Logging;

using Azure.Cosmos;

using System.Text.Json.Serialization;

using FinWise.LoanAgent.Models;

namespace FinWise.LoanAgent.Services;

/// <summary>
/// Handles persistence of LoanAgent context and conversation state to Cosmos DB.
/// </summary>
public class LoanAgentStateHandler(
    CosmosClient cosmosClient,
    string databaseName,
    string containerName,
    ILogger<LoanAgentStateHandler> logger)
{
    private readonly CosmosClient _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
    private readonly string _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
    private readonly string _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
    private readonly ILogger<LoanAgentStateHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Persists the loan context to Cosmos DB.
    /// </summary>
    public async Task<bool> PersistContextAsync(LoanAgentContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _cosmosClient.GetDatabase(_databaseName);
            var container = database.GetContainer(_containerName);

            var documentId = $"loan_{context.UserId}_{context.ConversationId}";
            var contextDocument = new LoanContextDocument
            {
                Id = documentId,
                PartitionKey = context.UserId,
                Context = context,
                Timestamp = DateTime.UtcNow,
                TTL = 604800 // 7 days
            };

            _ = await container.UpsertItemAsync(contextDocument, new PartitionKey(context.UserId), cancellationToken: cancellationToken);
            _logger.LogInformation("Persisted loan context for ConversationId={ConversationId}", context.ConversationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting loan context for ConversationId={ConversationId}", context.ConversationId);
            return false;
        }
    }

    /// <summary>
    /// Retrieves a previously persisted context from Cosmos DB.
    /// </summary>
    public async Task<LoanAgentContext?> GetContextAsync(string userId, string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _cosmosClient.GetDatabase(_databaseName);
            var container = database.GetContainer(_containerName);

            var documentId = $"loan_{userId}_{conversationId}";
            var response = await container.ReadItemAsync<LoanContextDocument>(
                documentId,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Retrieved loan context for ConversationId={ConversationId}", conversationId);
            return response.Value?.Context;
        }
        catch (Azure.Cosmos.CosmosException ex) when (ex.Status == 404)
        {
            _logger.LogDebug("Context not found for ConversationId={ConversationId}", conversationId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan context for ConversationId={ConversationId}", conversationId);
            return null;
        }
    }

    /// <summary>
    /// Deletes a context document from Cosmos DB.
    /// </summary>
    public async Task<bool> DeleteContextAsync(string userId, string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _cosmosClient.GetDatabase(_databaseName);
            var container = database.GetContainer(_containerName);

            var documentId = $"loan_{userId}_{conversationId}";
            _ = await container.DeleteItemAsync<LoanContextDocument>(
                documentId,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Deleted loan context for ConversationId={ConversationId}", conversationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting loan context for ConversationId={ConversationId}", conversationId);
            return false;
        }
    }

    /// <summary>
    /// Internal document model for Cosmos DB.
    /// </summary>
    private class LoanContextDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("partitionKey")]
        public string PartitionKey { get; set; } = string.Empty;

        [JsonPropertyName("context")]
        public LoanAgentContext Context { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("ttl")]
        public int TTL { get; set; }
    }
}
