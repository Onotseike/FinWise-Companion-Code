using FinWise.BudgetingAgent.ToshlApi.Contracts;
using FinWise.Shared.Core;

namespace FinWise.BudgetingAgent.ToshlApi.Modules;

public class ToshlAgent(
    AgentOptions agentOptions,
    IUserProfileModule user,
    IBankConnectionsModule bankConnections,
    ITransactionsModule transactions,
    IBudgetsModule budgets,
    IPlanningModule planning,
    ICurrencyModule currency,
    ICategoriesModule categories) : IToshlAgent
{
    public AgentOptions AgentOptions { get; } = agentOptions ?? throw new ArgumentNullException(nameof(agentOptions));
    public IUserProfileModule User { get; } = user ?? throw new ArgumentNullException(nameof(user));
    public IBankConnectionsModule BankConnections { get; } = bankConnections ?? throw new ArgumentNullException(nameof(bankConnections));
    public ITransactionsModule Transactions { get; } = transactions ?? throw new ArgumentNullException(nameof(transactions));
    public IBudgetsModule Budgets { get; } = budgets ?? throw new ArgumentNullException(nameof(budgets));
    public IPlanningModule Planning { get; } = planning ?? throw new ArgumentNullException(nameof(planning));
    public ICurrencyModule Currency { get; } = currency ?? throw new ArgumentNullException(nameof(currency));
    public ICategoriesModule Categories { get; } = categories ?? throw new ArgumentNullException(nameof(categories));
}