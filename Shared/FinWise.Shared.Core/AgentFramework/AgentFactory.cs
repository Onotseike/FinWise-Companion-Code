using Azure;
using Azure.AI.OpenAI;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OpenAI.Chat;

namespace FinWise.Shared.Core.AgentFramework;

/// <summary>
/// Factory for creating standardized AIAgent instances with consistent configuration.
/// Handles Azure OpenAI client creation, tool registration, and agent initialization.
/// </summary>
public static class AgentFactory
{
    /// <summary>
    /// Creates an AIAgent instance with the provided configuration and tools.
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="agentOptions">Agent identity and behavior options</param>
    /// <param name="tools">Optional list of tools/functions available to the agent</param>
    /// <param name="loggerFactory">Logger factory for diagnostics</param>
    /// <param name="services">Service provider for dependency injection</param>
    /// <returns>A configured AIAgent instance</returns>
    /// <exception cref="ArgumentException">Thrown if required configuration is missing</exception>
    public static AIAgent CreateAgent(
        IConfiguration configuration,
        AgentOptions agentOptions,
        IList<AITool>? tools = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        string endpoint = configuration["Values:AzureOpenAIEndpoint"]
                       ?? throw new ArgumentNullException(nameof(configuration), "AzureOpenAIEndpoint not configured");
        string key = configuration["Values:AzureOpenAIKey"]
                  ?? throw new ArgumentNullException(nameof(configuration), "AzureOpenAIKey not configured");
        string deployment = configuration["Values:AzureOpenAIChatDeploymentName"]
                 ?? configuration["AzureOpenAIChatDeploymentName"]
                 ?? configuration["Values:AzureOpenAIDeploymentName"]
                 ?? configuration["AzureOpenAIDeploymentName"]
                         ?? throw new ArgumentNullException(nameof(configuration), "AzureOpenAIDeploymentName not configured");

        return CreateAgent(
            endpoint,
            key,
            deployment,
            agentOptions,
            tools,
            loggerFactory,
            services);
    }

    /// <summary>
    /// Creates an AIAgent instance with explicit endpoint credentials.
    /// </summary>
    /// <param name="endpoint">Azure OpenAI endpoint URL</param>
    /// <param name="apiKey">Azure OpenAI API key</param>
    /// <param name="deploymentName">Azure OpenAI deployment name (model)</param>
    /// <param name="agentOptions">Agent identity and behavior options</param>
    /// <param name="tools">Optional list of tools/functions available to the agent</param>
    /// <param name="loggerFactory">Logger factory for diagnostics</param>
    /// <param name="services">Service provider for dependency injection</param>
    /// <returns>A configured AIAgent instance</returns>
    public static AIAgent CreateAgent(
        string endpoint,
        string apiKey,
        string deploymentName,
        AgentOptions agentOptions,
        IList<AITool>? tools = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be empty", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be empty", nameof(apiKey));
        }

        if (string.IsNullOrWhiteSpace(deploymentName))
        {
            throw new ArgumentException("Deployment name cannot be empty", nameof(deploymentName));
        }

        ArgumentNullException.ThrowIfNull(agentOptions);

        AzureOpenAIClient client = new(new Uri(endpoint), new AzureKeyCredential(apiKey));

        return client.GetChatClient(deploymentName)
            .AsAIAgent(
                instructions: agentOptions.Instructions,
                name: agentOptions.Id,
                description: agentOptions.Description,
                tools: tools,
                loggerFactory: loggerFactory,
                services: services);
    }

    /// <summary>
    /// Creates an AIAgent from the standardized AgentConfiguration.
    /// </summary>
    /// <param name="config">The unified agent configuration</param>
    /// <param name="tools">Optional list of tools available to the agent</param>
    /// <param name="loggerFactory">Logger factory for diagnostics</param>
    /// <param name="services">Service provider for dependency injection</param>
    /// <returns>A configured AIAgent instance</returns>
    /// <exception cref="InvalidOperationException">Thrown if configuration is invalid</exception>
    public static AIAgent CreateAgentFromConfiguration(
        Configuration.AgentConfiguration config,
        IList<AITool>? tools = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        config.Validate();

        var agentOptions = new AgentOptions(
            managementEndPoint: new Uri(config.Agent.ManagementEndpoint),
            id: config.Agent.Id ?? throw new InvalidOperationException("Agent.Id is required"),
            heartBeatFrequency: TimeSpan.FromSeconds(config.Agent.HeartbeatFrequencySeconds),
            instructions: config.Agent.Instructions,
            description: config.Agent.Description);

        return CreateAgent(
            config.AzureOpenAI.Endpoint ?? throw new InvalidOperationException("AzureOpenAI.Endpoint is required"),
            config.AzureOpenAI.ApiKey ?? throw new InvalidOperationException("AzureOpenAI.ApiKey is required"),
            config.AzureOpenAI.DeploymentName ?? throw new InvalidOperationException("AzureOpenAI.DeploymentName is required"),
            agentOptions,
            tools,
            loggerFactory,
            services);
    }
}
