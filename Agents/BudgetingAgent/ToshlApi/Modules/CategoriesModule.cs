using FinWise.BudgetingAgent.ToshlApi.Contracts;

using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent.ToshlApi.Modules;

public class CategoriesModule : ICategoriesModule
{
    private readonly IToshlApiClient _apiClient;
    private readonly ILogger<CategoriesModule> _logger;
    public CategoriesModule(IToshlApiClient apiClient, ILogger<CategoriesModule> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _logger.LogDebug("Categories module initialized");
    }

    public Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        _logger.LogDebug("Fetching categories");
        return _apiClient.GetAsync<IEnumerable<Category>>("/categories")
            .ContinueWith(task =>
            {
                if (task.Result.Status == 200)
                {
                    _logger.LogInformation("Categories fetched successfully");
                    return task.Result.Data;
                }
                else
                {
                    _logger.LogError("Failed to fetch categories: {Error}", task.Result.Data);
                    throw new Exception($"Failed to fetch categories: {task.Result.Data}");
                }
            });
    }
    public Task<IEnumerable<Tag>> GetTagsAsync()
    {
        _logger.LogDebug("Fetching tags");
        return _apiClient.GetAsync<IEnumerable<Tag>>("/tags")
            .ContinueWith(task =>
            {
                if (task.Result.Status == 200)
                {
                    _logger.LogInformation("Tags fetched successfully");
                    return task.Result.Data;
                }
                else
                {
                    _logger.LogError("Failed to fetch tags: {Error}", task.Result.Data);
                    throw new Exception($"Failed to fetch tags: {task.Result.Data}");
                }
            });
    }
}
