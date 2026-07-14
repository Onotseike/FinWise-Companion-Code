namespace FinWise.BudgetingAgent.ToshlApi.Contracts;

public interface IBankConnectionsModule
{
    Task<IEnumerable<BankConnection>> GetAllAsync();
    //Task RefreshAsync(string connectionId);
}
