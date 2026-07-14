namespace FinWise.BudgetingAgent.ToshlApi.Contracts;

public interface IBudgetsModule
{
    Task<IEnumerable<Budget>> GetBudgetsAsync(string? from = null, string? to = null);
    Task<IEnumerable<Category>> GetBudgetsWithCategories();
    //Task<Budget> CreateOrUpdateAsync(Budget input);
}
