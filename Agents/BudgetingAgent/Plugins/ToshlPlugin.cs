using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

using FinWise.BudgetingAgent.ToshlApi;

using FinWise.BudgetingAgent.ToshlApi.Contracts;
using FinWise.BudgetingAgent.ToshlApi.Modules.Helpers;
using Microsoft.Extensions.AI;

namespace FinWise.BudgetingAgent.Plugins;

public class ToshlPlugin(IToshlAgent toshlAgent)
{
    private readonly IToshlAgent _toshlAgent = toshlAgent ?? throw new ArgumentNullException(nameof(toshlAgent));
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [Description("Get the user profile information from Toshl")]
    public async Task<string> GetUserProfileAsync()
    {
        User profile = await _toshlAgent.User.GetProfileAsync();
        return JsonSerializer.Serialize(profile, s_jsonOptions);
    }

    [Description("Get account summary with optional date range")]
    public async Task<string> GetAccountSummaryAsync(
        [Description("Start date in YYYY-MM-DD format")] string? fromDate = null,
        [Description("End date in YYYY-MM-DD format")] string? toDate = null)
    {
        Summary summary = await _toshlAgent.User.GetAccountSummaryAsync(ModuleHelpers.NormalizeDate(fromDate), ModuleHelpers.NormalizeDate(toDate));
        return JsonSerializer.Serialize(summary, s_jsonOptions);
    }

    [Description("Get entries within a date range, optionally filtered by type: expense, income, transaction")]
    public async Task<string> GetTransactionsAsync(
        [Description("Start date in YYYY-MM-DD format")] string? fromDate = null,
        [Description("End date in YYYY-MM-DD format")] string? toDate = null,
        [Description("Optional entry type filter: expense, income, transaction")] string? type = null)
    {
        IEnumerable<Entry> transactions = await _toshlAgent.Transactions.GetRangeAsync(ModuleHelpers.NormalizeDate(fromDate), ModuleHelpers.NormalizeDate(toDate), type);
        return JsonSerializer.Serialize(transactions, s_jsonOptions);
    }

    [Description("Get all budgets with optional date range")]
    public async Task<string> GetBudgetsAsync(
        [Description("Start date in YYYY-MM-DD format")] string? fromDate = null,
        [Description("End date in YYYY-MM-DD format")] string? toDate = null)
    {
        IEnumerable<Budget> budgets = await _toshlAgent.Budgets.GetBudgetsAsync(ModuleHelpers.NormalizeDate(fromDate), ModuleHelpers.NormalizeDate(toDate));
        return JsonSerializer.Serialize(budgets, s_jsonOptions);
    }

    [Description("Get all expense categories")]
    public async Task<string> GetCategoriesAsync()
    {
        IEnumerable<Category> categories = await _toshlAgent.Categories.GetCategoriesAsync();
        return JsonSerializer.Serialize(categories, s_jsonOptions);
    }

    [Description("Estimate gross income from entries and categories for a date range")]
    public async Task<string> GetEstimatedIncomeAsync(
        [Description("Start date in YYYY-MM-DD format")] string? fromDate = null,
        [Description("End date in YYYY-MM-DD format")] string? toDate = null)
    {
        (string normalizedFrom, string normalizedTo) = ModuleHelpers.NormalizeDateRange(fromDate, toDate);

        IEnumerable<Entry> transactions = await _toshlAgent.Transactions.GetRangeAsync(normalizedFrom, normalizedTo);
        IEnumerable<Category> categories = await _toshlAgent.Categories.GetCategoriesAsync();

        Dictionary<string, string> categoryLookup = categories
            .Where(c => !string.IsNullOrWhiteSpace(c.Id))
            .ToDictionary(c => c.Id, c => c.Type, StringComparer.OrdinalIgnoreCase);

        List<Entry> incomeEntries = transactions
            .Where(e => e.Transaction is null)
            .Where(e =>
                !string.IsNullOrWhiteSpace(e.Category) && categoryLookup.TryGetValue(e.Category, out string? categoryType)
                    ? string.Equals(categoryType, "income", StringComparison.OrdinalIgnoreCase)
                    : e.Amount > 0)
            .ToList();

        decimal grossIncome = incomeEntries.Sum(e => e.Amount > 0 ? e.Amount : Math.Abs(e.Amount));

        int dayCount = 30;
        if (DateOnly.TryParseExact(normalizedFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly from)
            && DateOnly.TryParseExact(normalizedTo, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly to)
            && to.DayNumber >= from.DayNumber)
        {
            dayCount = (to.DayNumber - from.DayNumber) + 1;
        }

        decimal roughMonthlyIncome = dayCount > 0 ? grossIncome * 30m / dayCount : grossIncome;

        var payload = new
        {
            from = normalizedFrom,
            to = normalizedTo,
            estimatedGrossIncome = grossIncome,
            roughMonthlyIncome,
            incomeEntryCount = incomeEntries.Count,
            methodology = "Income categories are preferred; positive amounts are used as fallback. Transfer entries are excluded."
        };

        return JsonSerializer.Serialize(payload, s_jsonOptions);
    }

    [Description("Get financial planning forecast within a date range")]
    public async Task<string> GetPlanningAsync(
        [Description("Start date in YYYY-MM-DD format")] string? fromDate = null,
        [Description("End date in YYYY-MM-DD format")] string? toDate = null)
    {
        PlanningOverview planning = await _toshlAgent.Planning.ForecastAsync(ModuleHelpers.NormalizeDate(fromDate), ModuleHelpers.NormalizeDate(toDate));
        return JsonSerializer.Serialize(planning, s_jsonOptions);
    }

    [Description("Get all bank connections")]
    public async Task<string> GetBankConnectionsAsync()
    {
        IEnumerable<BankConnection> connections = await _toshlAgent.BankConnections.GetAllAsync();
        return JsonSerializer.Serialize(connections, s_jsonOptions);
    }

    [Description("Get all supported currencies")]
    public async Task<string> GetCurrenciesAsync()
    {
        IDictionary<string, SupportedCurrency> currencies = await _toshlAgent.Currency.GetSupportedCurrenciesAsync();
        return JsonSerializer.Serialize(currencies, s_jsonOptions);
    }

    public IList<AITool> GetAllTools() =>
    [
        AIFunctionFactory.Create(this.GetUserProfileAsync),
        AIFunctionFactory.Create(this.GetAccountSummaryAsync),
        AIFunctionFactory.Create(this.GetTransactionsAsync),
        AIFunctionFactory.Create(this.GetBudgetsAsync),
        AIFunctionFactory.Create(this.GetCategoriesAsync),
        AIFunctionFactory.Create(this.GetEstimatedIncomeAsync),
        AIFunctionFactory.Create(this.GetPlanningAsync),
        AIFunctionFactory.Create(this.GetBankConnectionsAsync),
        AIFunctionFactory.Create(this.GetCurrenciesAsync)
    ];
}