using FinWise.BudgetingAgent.ToshlApi.Contracts;

using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent.ToshlApi.Modules;

public class CurrencyModule : ICurrencyModule
{
    private readonly IToshlApiClient _apiClient;
    private readonly ILogger<CurrencyModule> _logger;
    public CurrencyModule(IToshlApiClient apiClient, ILogger<CurrencyModule> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _logger.LogDebug("Currency module initialized");
    }
    public Task<IDictionary<string, SupportedCurrency>> GetSupportedCurrenciesAsync()
    {
        _logger.LogDebug("Fetching supported currencies");
        return _apiClient.GetAsync<IDictionary<string,SupportedCurrency>>("/currencies")
            .ContinueWith(task =>
            {
                if (task.Result.Status == 200)
                {
                    _logger.LogInformation("Supported currencies fetched successfully");
                    return task.Result.Data;
                }
                else
                {
                    _logger.LogError("Failed to fetch supported currencies: {Error}", task.Result.Data);
                    throw new Exception($"Failed to fetch supported currencies: {task.Result.Data}");
                }
            });
    }

}
