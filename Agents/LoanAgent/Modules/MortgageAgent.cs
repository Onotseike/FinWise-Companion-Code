using FinWise.LoanAgent.Contracts;
using FinWise.Shared.Core;

namespace FinWise.LoanAgent.Modules;

internal class MortgageAgent(
    AgentOptions agentOptions,
    IPropertyModule propertyModule) : IMortgageAgent
{
    public AgentOptions AgentOptions { get; } = agentOptions ?? throw new ArgumentNullException(nameof(agentOptions));
    public IPropertyModule Properties { get; } = propertyModule ?? throw new ArgumentNullException(nameof(propertyModule));
}
