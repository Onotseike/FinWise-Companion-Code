using Microsoft.Extensions.Logging;

using Azure.Cosmos;

using System.Text.Json.Serialization;

using FinWise.BudgetingAgent.Models;

namespace FinWise.BudgetingAgent.Services;

/// <summary>
/// Handles persistence of BudgetingAgent context and conversation state to Cosmos DB.
/// </summary>
public class BudgetingAgentStateHandler(
    CosmosClient cosmosClient,
    string databaseName,
    string containerName,
    ILogger<BudgetingAgentStateHandler> logger)
{
    private readonly CosmosClient _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
    private readonly string _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
    private readonly string _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
    private readonly ILogger<BudgetingAgentStateHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Persists the budgeting context to Cosmos DB.
    /// </summary>
    public async Task<bool> PersistContextAsync(BudgetingAgentContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _cosmosClient.GetDatabase(_databaseName);
            var container = database.GetContainer(_containerName);

            var documentId = $"budgeting_{context.UserId}_{context.ConversationId}";
            var contextDocument = new BudgetingContextDocument
            {
                Id = documentId,
                PartitionKey = context.UserId,
                Context = context,
                Timestamp = DateTime.UtcNow,
                TTL = 604800 // 7 days
            };

            await container.UpsertItemAsync(contextDocument, new PartitionKey(context.UserId), cancellationToken: cancellationToken);
            _logger.LogInformation("Persisted budgeting context for ConversationId={ConversationId}", context.ConversationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting budgeting context for ConversationId={ConversationId}", context.ConversationId);
            return false;
        }
    }

    /// <summary>
    /// Retrieves a previously persisted context from Cosmos DB.
    /// </summary>
    public async Task<BudgetingAgentContext?> GetContextAsync(string userId, string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _cosmosClient.GetDatabase(_databaseName);
            var container = database.GetContainer(_containerName);

            var documentId = $"budgeting_{userId}_{conversationId}";
            var response = await container.ReadItemAsync<BudgetingContextDocument>(
                documentId,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Retrieved budgeting context for ConversationId={ConversationId}", conversationId);
            return response.Value?.Context;
        }
        catch (Azure.Cosmos.CosmosException ex) when (ex.Status == 404)
        {
            _logger.LogDebug("Context not found for ConversationId={ConversationId}", conversationId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budgeting context for ConversationId={ConversationId}", conversationId);
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

            var documentId = $"budgeting_{userId}_{conversationId}";
            await container.DeleteItemAsync<BudgetingContextDocument>(
                documentId,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Deleted budgeting context for ConversationId={ConversationId}", conversationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting budgeting context for ConversationId={ConversationId}", conversationId);
            return false;
        }
    }

    /// <summary>
    /// Internal document model for Cosmos DB.
    /// </summary>
    private class BudgetingContextDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("partitionKey")]
        public string PartitionKey { get; set; } = string.Empty;

        [JsonPropertyName("context")]
        public BudgetingAgentContext Context { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("ttl")]
        public int TTL { get; set; }
    }
}
