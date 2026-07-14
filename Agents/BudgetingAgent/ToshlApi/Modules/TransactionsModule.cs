using System.Linq;

using FinWise.BudgetingAgent.ToshlApi.Contracts;
using FinWise.BudgetingAgent.ToshlApi.Modules.Helpers;

using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent.ToshlApi.Modules;

public class TransactionsModule : ITransactionsModule
{
    private readonly IToshlApiClient _apiClient;
    private readonly ILogger<TransactionsModule> _logger;
    public TransactionsModule(IToshlApiClient apiClient, ILogger<TransactionsModule> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _logger.LogDebug("Transactions module initialized");
    }

    public Task<IEnumerable<Entry>> GetRangeAsync(string? from = null, string? to = null, string? type = null)
    {
        (string normalizedFrom, string normalizedTo) = ModuleHelpers.NormalizeDateRange(from, to);
        string? normalizedType = NormalizeEntryType(type);

        _logger.LogDebug("Fetching transactions from {From} to {To} with type {Type}", normalizedFrom, normalizedTo, normalizedType ?? "all");
        var parameters = new Dictionary<string, string>
        {
            ["from"] = normalizedFrom,
            ["to"] = normalizedTo
        };

        return _apiClient.GetAsync<IEnumerable<Entry>>("/entries", parameters)
            .ContinueWith(task =>
            {
                if (task.Result.Status == 200)
                {
                    IEnumerable<Entry> entries = task.Result.Data;
                    IEnumerable<Entry> filteredEntries = FilterByType(entries, normalizedType);
                    _logger.LogInformation("Transactions fetched successfully");
                    return filteredEntries;
                }
                else
                {
                    _logger.LogError("Failed to fetch transactions: {Error}", task.Result.Data);
                    throw new Exception($"Failed to fetch transactions: {task.Result.Data}");
                }
            });
    }

    private static string? NormalizeEntryType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return null;

        string normalized = type.Trim().ToLowerInvariant();
        return normalized switch
        {
            "expense" or "income" or "transaction" => normalized,
            _ => throw new ArgumentException("Invalid type. Allowed values: expense, income, transaction.", nameof(type))
        };
    }

    private static IEnumerable<Entry> FilterByType(IEnumerable<Entry> entries, string? type) => type switch
    {
        "expense" => entries.Where(e => e.Transaction is null && e.Amount < 0),
        "income" => entries.Where(e => e.Transaction is null && e.Amount > 0),
        "transaction" => entries.Where(e => e.Transaction is not null),
        _ => entries
    };
}
