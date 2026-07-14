using FinWise.Shared.Core;

namespace FinWise.BudgetingAgent.ToshlApi.Contracts;

public interface IToshlAgent
{
    AgentOptions AgentOptions { get; }
    IUserProfileModule User { get; }
    IBankConnectionsModule BankConnections { get; }
    ITransactionsModule Transactions { get; }
    IBudgetsModule Budgets { get; }
    IPlanningModule Planning { get; }
    ICurrencyModule Currency { get; }
    ICategoriesModule Categories { get; }
}