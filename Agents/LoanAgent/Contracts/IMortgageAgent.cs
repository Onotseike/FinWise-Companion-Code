using FinWise.Shared.Core;

namespace FinWise.LoanAgent.Contracts;

public interface IMortgageAgent
{
    AgentOptions AgentOptions { get; }
    IPropertyModule Properties { get; }
}
