﻿using FinWise.BudgetingAgent.ToshlApi.Contracts;
using FinWise.BudgetingAgent.ToshlApi.Modules.Helpers;

using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent.ToshlApi.Modules;

public class PlanningModule : IPlanningModule
{
    private readonly IToshlApiClient _apiClient;
    private readonly ILogger<PlanningModule> _logger;
    public PlanningModule(IToshlApiClient apiClient, ILogger<PlanningModule> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _logger.LogDebug("Planning module initialized");
    }

    public Task<PlanningOverview> ForecastAsync(string? from = null, string? to = null)
    {
        (string normalizedFrom, string normalizedTo) = ModuleHelpers.NormalizeDateRange(from, to);

        _logger.LogDebug("Forecasting from {From} to {To}", normalizedFrom, normalizedTo);
        var parameters = new Dictionary<string, string>
        {
            ["from"] = normalizedFrom,
            ["to"] = normalizedTo
        };
        return _apiClient.GetAsync<PlanningOverview>("/planning", parameters)
            .ContinueWith(task =>
            {
                if (task.Result.Status == 200)
                {
                    _logger.LogInformation("Forecast fetched successfully");
                    return task.Result.Data;
                }
                else
                {
                    _logger.LogError("Failed to fetch forecast: {Error}", task.Result.Data);
                    throw new Exception($"Failed to fetch forecast: {task.Result.Data}");
                }
            });
    }
}
