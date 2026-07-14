using FinWise.BudgetingAgent.ToshlApi.Contracts;
using FinWise.BudgetingAgent.ToshlApi.Modules.Helpers;

using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent.ToshlApi.Modules;

public class BudgetsModule : IBudgetsModule
{
    private readonly IToshlApiClient _apiClient;
    private readonly ILogger<BudgetsModule> _logger;
    public BudgetsModule(IToshlApiClient apiClient, ILogger<BudgetsModule> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _logger.LogDebug("Budgets module initialized");
    }

    public Task<IEnumerable<Budget>> GetBudgetsAsync(string? from = null, string? to = null)
    {
        (string normalizedFrom, string normalizedTo) = ModuleHelpers.NormalizeDateRange(from, to);

        _logger.LogDebug("Fetching budgets from {From} to {To}", normalizedFrom, normalizedTo);
        var parameters = new Dictionary<string, string>
        {
            ["from"] = normalizedFrom,
            ["to"] = normalizedTo
        };
        return _apiClient.GetAsync<IEnumerable<Budget>>("/budgets", parameters)
            .ContinueWith(task =>
            {
                if (task.Result.Status == 200)
                {
                    _logger.LogInformation("Budgets fetched successfully");
                    return task.Result.Data;
                }
                else
                {
                    _logger.LogError("Failed to fetch budgets: {Error}", task.Result.Data);
                    throw new Exception($"Failed to fetch budgets: {task.Result.Data}");
                }
            });

    }

    public async Task<IEnumerable<Category>> GetBudgetsWithCategories()
    {
        _logger.LogDebug("Fetching budgets with categories"); 
        IEnumerable<Category> categories = await _apiClient.GetAsync<IEnumerable<Category>>("/categories")
            .ContinueWith(task =>
            {
                if (task.Result.Status == 200)
                {
                    _logger.LogInformation("Budgets with categories fetched successfully");
                    return task.Result.Data;
                }
                else
                {
                    _logger.LogError("Failed to fetch budgets with categories: {Error}", task.Result.Data);
                    throw new Exception($"Failed to fetch budgets with categories: {task.Result.Data}");
                }
            });
        return categories;
    }
}
