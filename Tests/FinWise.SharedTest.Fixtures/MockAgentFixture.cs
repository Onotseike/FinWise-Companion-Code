using Microsoft.Agents.AI;

using Moq;

namespace FinWise.SharedTest.Fixtures;

/// <summary>
/// Fixture for creating mock AIAgent instances for testing.
/// Allows configuration of tool responses and agent behavior.
/// </summary>
public class MockAgentFixture
{
    private readonly Mock<AIAgent> _mockAgent;

    public MockAgentFixture() => _mockAgent = new Mock<AIAgent>();

    /// <summary>
    /// Gets the underlying mock AIAgent.
    /// </summary>
    public Mock<AIAgent> Agent => _mockAgent;

    /// <summary>
    /// Configures the mock agent to return a specific response text when invoked.
    /// </summary>
    /// <param name="responseText">The text response to return</param>
    /// <returns>The fixture for method chaining</returns>
    public MockAgentFixture WithResponse(string responseText) =>
        // Note: AIAgent.AsFunction() call pattern would be mocked here
        // This is a placeholder for the actual invocation pattern once AIAgent's dynamic behavior is known
        this;

    /// <summary>
    /// Configures the mock agent to return a specific response with usage information.
    /// </summary>
    /// <param name="responseText">The text response</param>
    /// <param name="inputTokens">Number of input tokens</param>
    /// <param name="outputTokens">Number of output tokens</param>
    /// <returns>The fixture for method chaining</returns>
    public MockAgentFixture WithResponseAndUsage(string responseText, int inputTokens, int outputTokens) =>
        // Placeholder for tracking token usage
        this;

    /// <summary>
    /// Configures the mock agent to throw an exception when invoked.
    /// </summary>
    /// <param name="exception">The exception to throw</param>
    /// <returns>The fixture for method chaining</returns>
    public MockAgentFixture WithException(Exception exception)
    {
        _ = _mockAgent.Setup(a => a.GetType()).Throws(exception);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured mock AIAgent.
    /// </summary>
    /// <returns>The mock AIAgent instance</returns>
    public AIAgent Build() => _mockAgent.Object;
}

/// <summary>
/// Helper class for creating test configuration objects for agents.
/// </summary>
public static class TestConfigBuilder
{
    /// <summary>
    /// Creates a test AgentConfiguration for testing.
    /// </summary>
    /// <param name="agentId">The agent ID</param>
    /// <returns>A minimal AgentConfiguration suitable for testing</returns>
    public static FinWise.Shared.Core.Configuration.AgentConfiguration CreateTestConfiguration(string agentId = "test-agent") => new()
    {
        Agent = new FinWise.Shared.Core.Configuration.AgentIdentitySettings
        {
            Id = agentId,
            Name = $"Test {agentId}",
            Description = "Test agent configuration",
            Instructions = "You are a test agent",
            ManagementEndpoint = "http://localhost:7071",
            HeartbeatFrequencySeconds = 300
        },
        AzureOpenAI = new FinWise.Shared.Core.Configuration.AzureOpenAISettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key-12345",
            DeploymentName = "test-deployment",
            ApiVersion = "2024-10-21"
        },
        ServiceBus = new FinWise.Shared.Core.Configuration.ServiceBusSettings
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test;",
            TopicName = "agent-messages",
            SubscriptionName = agentId,
            MaxConcurrentCalls = 10,
            MaxAutoLockRenewalDurationSeconds = 300,
            MessageTimeoutSeconds = 30
        }
    };
}
