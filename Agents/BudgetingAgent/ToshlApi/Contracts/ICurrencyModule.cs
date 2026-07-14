namespace FinWise.BudgetingAgent.ToshlApi.Contracts;

public interface ICurrencyModule
{
    Task<IDictionary<string, SupportedCurrency>> GetSupportedCurrenciesAsync();
}
