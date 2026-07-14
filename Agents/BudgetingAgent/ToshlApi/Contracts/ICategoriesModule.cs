namespace FinWise.BudgetingAgent.ToshlApi.Contracts;

public interface ICategoriesModule
{
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<IEnumerable<Tag>> GetTagsAsync();
}
