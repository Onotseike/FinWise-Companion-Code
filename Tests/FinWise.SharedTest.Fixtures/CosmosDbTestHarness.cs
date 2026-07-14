using System.Collections.Concurrent;

namespace FinWise.SharedTest.Fixtures;

/// <summary>
/// In-memory test harness for Cosmos DB operations.
/// Simulates Cosmos DB containers and documents for testing state persistence.
/// </summary>
public class CosmosDbTestHarness : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Container> _containers = new();

    /// <summary>
    /// Gets or creates a container for testing.
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <returns>An in-memory container instance</returns>
    public Container GetOrCreateContainer(string containerName) => _containers.GetOrAdd(containerName, _ => new Container());

    /// <summary>
    /// Creates a container with a specific partition key for testing.
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="partitionKeyPath">The partition key path (e.g., "/userId")</param>
    /// <returns>An in-memory container instance</returns>
    public Container CreateContainer(string containerName, string partitionKeyPath)
    {
        var container = new Container { PartitionKeyPath = partitionKeyPath };
        _ = _containers.TryAdd(containerName, container);
        return container;
    }

    /// <summary>
    /// Gets a container by name.
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <returns>The container, or null if not found</returns>
    public Container? GetContainer(string containerName)
    {
        _ = _containers.TryGetValue(containerName, out var container);
        return container;
    }

    /// <summary>
    /// Gets the total number of documents across all containers.
    /// </summary>
    public int GetTotalDocumentCount() => _containers.Values.Sum(c => c.GetDocumentCount());

    /// <summary>
    /// Clears all containers.
    /// </summary>
    public void ClearAll() => _containers.Clear();

    /// <summary>
    /// Disposes the test harness.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        ClearAll();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Represents an in-memory Cosmos DB container.
    /// </summary>
    public class Container
    {
        private readonly ConcurrentDictionary<string, Document> _documents = new();

        /// <summary>
        /// Gets or sets the partition key path for this container.
        /// </summary>
        public string PartitionKeyPath { get; set; } = "/id";

        /// <summary>
        /// Inserts or updates a document.
        /// </summary>
        /// <param name="documentId">The document ID</param>
        /// <param name="document">The document data</param>
        public void UpsertDocument(string documentId, object document)
        {
            var doc = new Document
            {
                Id = documentId,
                Data = document,
                Timestamp = DateTime.UtcNow
            };

            _ = _documents.AddOrUpdate(documentId, doc, (_, _) => doc);
        }

        /// <summary>
        /// Retrieves a document by ID.
        /// </summary>
        /// <param name="documentId">The document ID</param>
        /// <returns>The document, or null if not found</returns>
        public Document? GetDocument(string documentId)
        {
            _ = _documents.TryGetValue(documentId, out var doc);
            return doc;
        }

        /// <summary>
        /// Deletes a document by ID.
        /// </summary>
        /// <param name="documentId">The document ID</param>
        /// <returns>True if deleted, false if not found</returns>
        public bool DeleteDocument(string documentId) => _documents.TryRemove(documentId, out _);

        /// <summary>
        /// Gets all documents in the container.
        /// </summary>
        /// <returns>A collection of documents</returns>
        public IEnumerable<Document> GetAllDocuments() => _documents.Values.OrderBy(d => d.Timestamp);

        /// <summary>
        /// Gets the number of documents in the container.
        /// </summary>
        public int GetDocumentCount() => _documents.Count;

        /// <summary>
        /// Clears all documents from the container.
        /// </summary>
        public void Clear() => _documents.Clear();

        /// <summary>
        /// Queries documents by a predicate.
        /// </summary>
        /// <param name="predicate">The query predicate</param>
        /// <returns>Matching documents</returns>
        public IEnumerable<Document> Query(Func<Document, bool> predicate) => _documents.Values.Where(predicate);
    }

    /// <summary>
    /// Represents an in-memory document.
    /// </summary>
    public class Document
    {
        /// <summary>
        /// Gets or sets the document ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the document data.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// Gets or sets the document creation timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the document's partition key value.
        /// </summary>
        public string? PartitionKey { get; set; }
    }
}

/// <summary>
/// Test data builder for Cosmos DB documents.
/// </summary>
public static class CosmosDbTestDataBuilder
{
    /// <summary>
    /// Creates a test conversation document.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="messageCount">Number of test messages to include</param>
    /// <returns>A dictionary representing a conversation document</returns>
    public static Dictionary<string, object> CreateTestConversation(
        string userId,
        string conversationId,
        int messageCount = 5)
    {
        var messages = Enumerable.Range(1, messageCount)
            .Select(i => new
            {
                role = i % 2 == 1 ? "user" : "assistant",
                content = $"Test message {i}",
                timestamp = DateTime.UtcNow.AddMinutes(-i)
            })
            .Cast<object>()
            .ToList();

        return new Dictionary<string, object>
        {
            { "id", conversationId },
            { "userId", userId },
            { "conversationId", conversationId },
            { "messages", messages },
            { "createdAt", DateTime.UtcNow },
            { "lastActiveAt", DateTime.UtcNow }
        };
    }

    /// <summary>
    /// Creates a test agent state document.
    /// </summary>
    /// <param name="agentId">The agent ID</param>
    /// <param name="conversationId">The conversation ID</param>
    /// <returns>A dictionary representing an agent state document</returns>
    public static Dictionary<string, object> CreateTestAgentState(
        string agentId,
        string conversationId) => new()
        {
            { "id", $"{agentId}_{conversationId}" },
            { "agentId", agentId },
            { "conversationId", conversationId },
            { "tools", new Dictionary<string, object>() },
            { "memory", new Dictionary<string, object>() },
            { "lastUpdated", DateTime.UtcNow }
        };
}
