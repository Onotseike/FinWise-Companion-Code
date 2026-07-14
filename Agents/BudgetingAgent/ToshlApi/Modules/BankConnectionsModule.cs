using FinWise.BudgetingAgent.ToshlApi.Contracts;

using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent.ToshlApi.Modules;

public class BankConnectionsModule : IBankConnectionsModule
{
    private readonly IToshlApiClient _apiClient;
    private readonly ILogger<BankConnectionsModule> _logger;

    public BankConnectionsModule(IToshlApiClient apiClient, ILogger<BankConnectionsModule> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _logger.LogDebug("Bank connections module initialized");
    }

    public async Task<IEnumerable<BankConnection>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all bank connections");
        return await _apiClient.GetAsync<IEnumerable<BankConnection>>("/bank/connections")
            .ContinueWith(task =>
            {
                if (task.Result.Status == 200)
                {
                    _logger.LogInformation("Bank connections fetched successfully");
                    return task.Result.Data;
                }
                else
                {
                    _logger.LogError("Failed to fetch bank connections: {Error}", task.Result.Data);
                    throw new Exception($"Failed to fetch bank connections: {task.Result.Data}");
                }
            });

    }
}
