namespace FinWise.Shared.Core;

/// <summary>
/// Configuration options for a FinWise agent.
/// Contains identity, endpoint, heartbeat frequency, and behavior instructions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the AgentOptions class.
/// </remarks>
/// <param name="managementEndPoint">The management endpoint URI</param>
/// <param name="id">Unique agent identifier</param>
/// <param name="heartBeatFrequency">Heartbeat signal frequency</param>
/// <param name="instructions">Optional system instructions</param>
/// <param name="description">Optional agent description</param>
/// <remarks>
/// Validation is performed by AgentConfiguration.Validate() before this instance is created,
/// typically via AgentFactory.CreateAgentFromConfiguration(). This ensures all required values
/// are present and valid before the Options instance is constructed for dependency injection.
/// </remarks>
public class AgentOptions(
    Uri managementEndPoint,
    string id,
    TimeSpan heartBeatFrequency,
    string? instructions = null,
    string? description = null)
{
    /// <summary>
    /// The management endpoint URI for the agent.
    /// </summary>
    public Uri ManagementEndpoint { get; set; } = managementEndPoint;

    /// <summary>
    /// Unique identifier for this agent.
    /// </summary>
    public string Id { get; set; } = id.Trim();

    /// <summary>
    /// Frequency at which the agent sends heartbeat signals to the management endpoint.
    /// </summary>
    public TimeSpan HeartbeatFrequency { get; set; } = heartBeatFrequency;

    /// <summary>
    /// Optional system instructions for agent behavior and personality.
    /// </summary>
    public string? Instructions { get; set; } = instructions;

    /// <summary>
    /// Optional description of the agent's purpose and capabilities.
    /// </summary>
    public string? Description { get; set; } = description;
}
