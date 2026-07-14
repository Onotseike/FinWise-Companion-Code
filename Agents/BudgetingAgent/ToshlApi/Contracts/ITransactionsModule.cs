namespace FinWise.BudgetingAgent.ToshlApi.Contracts;

public interface ITransactionsModule
{
    Task<IEnumerable<Entry>> GetRangeAsync(string? from = null, string? to = null, string? type = null);
}
