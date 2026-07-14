using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

using FinWise.BudgetingAgent.ToshlApi;
using FinWise.BudgetingAgent.ToshlApi.Contracts;
using FinWise.BudgetingAgent.ToshlApi.Modules.Helpers;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent;

public class ToshlUserFunctions(IToshlAgent toshlAgent, ILogger<ToshlUserFunctions> logger)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    #region Helper Methods

    private static string? FormatDate(string? date)
    {
        return ModuleHelpers.NormalizeDate(date);
    }

    #endregion

    #region Private Function Methods (Used by both MCP and SK)

    [Description("Get the user profile information from Toshl")]
    private async Task<string> GetUserProfileAsync()
    {
        try
        {
            logger.LogInformation("Executing GetUserProfile");
            User profile = await toshlAgent.User.GetProfileAsync();
            return JsonSerializer.Serialize(profile, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetUserProfile");
            throw new InvalidOperationException($"Error fetching user profile: {ex.Message}", ex);
        }
    }

    [Description("Get account summary with optional date range")]
    private async Task<string> GetAccountSummaryAsync(
        [Description("Start date in YYYY-MM-DD format")] string? fromDate = null,
        [Description("End date in YYYY-MM-DD format")] string? toDate = null)
    {
        try
        {
            logger.LogInformation("Executing GetAccountSummary");
            Summary summary = await toshlAgent.User.GetAccountSummaryAsync(FormatDate(fromDate), FormatDate(toDate));
            return JsonSerializer.Serialize(summary, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetAccountSummary");
            throw new InvalidOperationException($"Error fetching account summary: {ex.Message}", ex);
        }
    }

    [Description("Get available payment types")]
    private async Task<string> GetPaymentTypesAsync()
    {
        try
        {
            logger.LogInformation("Executing GetPaymentTypes");
            PaymentType[] paymentTypes = await toshlAgent.User.GetPaymentTypesAsync();
            return JsonSerializer.Serialize(paymentTypes, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetPaymentTypes");
            throw new InvalidOperationException($"Error fetching payment types: {ex.Message}", ex);
        }
    }

    [Description("Get user payments")]
    private async Task<string> GetPaymentsAsync()
    {
        try
        {
            logger.LogInformation("Executing GetPayments");
            Payment[] payments = await toshlAgent.User.GetPaymentsAsync();
            return JsonSerializer.Serialize(payments, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetPayments");
            throw new InvalidOperationException($"Error fetching payments: {ex.Message}", ex);
        }
    }

    [Description("Get all bank connections")]
    private async Task<string> GetBankConnectionsAsync()
    {
        try
        {
            logger.LogInformation("Executing GetBankConnections");
            IEnumerable<BankConnection> connections = await toshlAgent.BankConnections.GetAllAsync();
            return JsonSerializer.Serialize(connections, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetBankConnections");
            throw new InvalidOperationException($"Error fetching bank connections: {ex.Message}", ex);
        }
    }

    [Description("Get entries within a date range, optionally filtered by type: expense, income, transaction")]
    private async Task<string> GetTransactionsAsync(
        [Description("Start date in YYYY-MM-DD format")] string? fromDate = null,
        [Description("End date in YYYY-MM-DD format")] string? toDate = null,
        [Description("Optional entry type filter: expense, income, transaction")] string? type = null)
    {
        try
        {
            logger.LogInformation("Executing GetTransactions");
            IEnumerable<Entry> transactions = await toshlAgent.Transactions.GetRangeAsync(FormatDate(fromDate), FormatDate(toDate), type);
            return JsonSerializer.Serialize(transactions, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetTransactions");
            throw new InvalidOperationException($"Error fetching transactions: {ex.Message}", ex);
        }
    }

    [Description("Get all budgets with optional date range")]
    private async Task<string> GetBudgetsAsync(
        [Description("Start date in YYYY-MM-DD format")] string? fromDate = null,
        [Description("End date in YYYY-MM-DD format")] string? toDate = null)
    {
        try
        {
            logger.LogInformation("Executing GetBudgets");
            IEnumerable<Budget> budgets = await toshlAgent.Budgets.GetBudgetsAsync(FormatDate(fromDate), FormatDate(toDate));
            return JsonSerializer.Serialize(budgets, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetBudgets");
            throw new InvalidOperationException($"Error fetching budgets: {ex.Message}", ex);
        }
    }

    [Description("Get financial planning forecast within a date range")]
    private async Task<string> GetPlanningAsync(
        [Description("Start date in YYYY-MM-DD format")] string? fromDate = null,
        [Description("End date in YYYY-MM-DD format")] string? toDate = null)
    {
        try
        {
            logger.LogInformation("Executing GetPlanning");
            PlanningOverview planning = await toshlAgent.Planning.ForecastAsync(FormatDate(fromDate), FormatDate(toDate));
            return JsonSerializer.Serialize(planning, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetPlanning");
            throw new InvalidOperationException($"Error fetching planning data: {ex.Message}", ex);
        }
    }

    [Description("Get all expense categories")]
    private async Task<string> GetCategoriesAsync()
    {
        try
        {
            logger.LogInformation("Executing GetCategories");
            IEnumerable<Category> categories = await toshlAgent.Categories.GetCategoriesAsync();
            return JsonSerializer.Serialize(categories, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetCategories");
            throw new InvalidOperationException($"Error fetching categories: {ex.Message}", ex);
        }
    }

    [Description("Get all category tags")]
    private async Task<string> GetCategoryTagsAsync()
    {
        try
        {
            logger.LogInformation("Executing GetCategoryTags");
            IEnumerable<Tag> tags = await toshlAgent.Categories.GetTagsAsync();
            return JsonSerializer.Serialize(tags, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetCategoryTags");
            throw new InvalidOperationException($"Error fetching category tags: {ex.Message}", ex);
        }
    }

    [Description("Get all supported currencies")]
    private async Task<string> GetCurrenciesAsync()
    {
        try
        {
            logger.LogInformation("Executing GetCurrencies");
            IDictionary<string, SupportedCurrency> currencies = await toshlAgent.Currency.GetSupportedCurrenciesAsync();
            return JsonSerializer.Serialize(currencies, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetCurrencies");
            throw new InvalidOperationException($"Error fetching currencies: {ex.Message}", ex);
        }
    }

    [Description("Estimate gross income from entries and categories for a date range")]
    private async Task<string> GetEstimatedIncomeAsync(
        [Description("Start date in YYYY-MM-DD format")] string? fromDate = null,
        [Description("End date in YYYY-MM-DD format")] string? toDate = null)
    {
        try
        {
            logger.LogInformation("Executing GetEstimatedIncome");
            (string normalizedFrom, string normalizedTo) = ModuleHelpers.NormalizeDateRange(fromDate, toDate);

            IEnumerable<Entry> transactions = await toshlAgent.Transactions.GetRangeAsync(normalizedFrom, normalizedTo, "income");
            IEnumerable<Category> categories = await toshlAgent.Categories.GetCategoriesAsync();

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GetEstimatedIncome");
            throw new InvalidOperationException($"Error estimating income: {ex.Message}", ex);
        }
    }

    #endregion

    #region MCP Trigger User Functions (Now calling the async methods)

    [Function(nameof(GetUserProfile))]
    public async Task<string> GetUserProfile(
        [McpToolTrigger("getUserProfile", "Get the user profile information from Toshl")]
        ToolInvocationContext context) => await GetUserProfileAsync();

    [Function(nameof(GetAccountSummary))]
    public async Task<string> GetAccountSummary(
        [McpToolTrigger("getAccountSummary", "Get account summary with optional date range")]
        ToolInvocationContext context,
        [McpToolProperty("from", "Start date (YYYY-MM-DD)", true)] string fromDate,
        [McpToolProperty("to", "End date (YYYY-MM-DD)")] string toDate)
    {
        // Extract parameters from context
        Dictionary<string, object>? arguments = context.Arguments;
        string? from = arguments?.ContainsKey("from") == true ? arguments["from"]?.ToString() : fromDate;
        string? to = arguments?.ContainsKey("to") == true ? arguments["to"]?.ToString() : toDate;

        return await GetAccountSummaryAsync(from, to);
    }

    [Function(nameof(GetPaymentTypes))]
    public async Task<string> GetPaymentTypes(
        [McpToolTrigger("getPaymentTypes", "Get available payment types")]
        ToolInvocationContext context) => await GetPaymentTypesAsync();

    [Function(nameof(GetPayments))]
    public async Task<string> GetPayments(
        [McpToolTrigger("getPayments", "Get user payments")]
        ToolInvocationContext context) => await GetPaymentsAsync();

    [Function(nameof(GetBankConnections))]
    public async Task<string> GetBankConnections(
        [McpToolTrigger("getBankConnections", "Get all bank connections")]
        ToolInvocationContext context) => await GetBankConnectionsAsync();

    [Function(nameof(GetTransactions))]
    public async Task<string> GetTransactions(
        [McpToolTrigger("getTransactions", "Get entries within a date range, optionally filtered by type")]
        ToolInvocationContext context,
        [McpToolProperty("from", "Start date (YYYY-MM-DD)", true)] string fromDate,
        [McpToolProperty("to", "End date (YYYY-MM-DD)")] string toDate,
        [McpToolProperty("type", "Optional: expense, income, transaction")] string? type = null)
    {
        Dictionary<string, object>? arguments = context.Arguments;
        string? from = arguments?.ContainsKey("from") == true ? arguments["from"]?.ToString() : fromDate;
        string? to = arguments?.ContainsKey("to") == true ? arguments["to"]?.ToString() : toDate;
        string? entryType = arguments?.ContainsKey("type") == true ? arguments["type"]?.ToString() : type;

        return await GetTransactionsAsync(from, to, entryType);
    }

    [Function(nameof(GetBudgets))]
    public async Task<string> GetBudgets(
        [McpToolTrigger("getBudgets", "Get all budgets")]
        ToolInvocationContext context,
        [McpToolProperty("from", "Start date (YYYY-MM-DD)", true)] string fromDate,
        [McpToolProperty("to", "End date (YYYY-MM-DD)")] string toDate)
    {
        Dictionary<string, object>? arguments = context.Arguments;
        string? from = arguments?.ContainsKey("from") == true ? arguments["from"]?.ToString() : fromDate;
        string? to = arguments?.ContainsKey("to") == true ? arguments["to"]?.ToString() : toDate;

        return await GetBudgetsAsync(from, to);
    }

    [Function(nameof(GetPlanning))]
    public async Task<string> GetPlanning(
        [McpToolTrigger("getPlanning", "Get financial planning forecast within a date range")]
        ToolInvocationContext context,
        [McpToolProperty("from", "Start date (YYYY-MM-DD)", true)] string fromDate,
        [McpToolProperty("to", "End date (YYYY-MM-DD)")] string toDate)
    {
        Dictionary<string, object>? arguments = context.Arguments;
        string? from = arguments?.ContainsKey("from") == true ? arguments["from"]?.ToString() : fromDate;
        string? to = arguments?.ContainsKey("to") == true ? arguments["to"]?.ToString() : toDate;

        return await GetPlanningAsync(from, to);
    }

    [Function(nameof(GetCategories))]
    public async Task<string> GetCategories(
        [McpToolTrigger("getCategories", "Get all categories")]
        ToolInvocationContext context) => await GetCategoriesAsync();

    [Function(nameof(GetCategoryTags))]
    public async Task<string> GetCategoryTags(
        [McpToolTrigger("getCategoryTags", "Get all category tags")]
        ToolInvocationContext context) => await GetCategoryTagsAsync();

    [Function(nameof(GetCurrencies))]
    public async Task<string> GetCurrencies(
        [McpToolTrigger("getCurrencies", "Get all supported currencies")]
        ToolInvocationContext context) => await GetCurrenciesAsync();

    [Function(nameof(GetEstimatedIncome))]
    public async Task<string> GetEstimatedIncome(
        [McpToolTrigger("getEstimatedIncome", "Estimate rough income from entries and categories")]
        ToolInvocationContext context,
        [McpToolProperty("from", "Start date (YYYY-MM-DD)", true)] string fromDate,
        [McpToolProperty("to", "End date (YYYY-MM-DD)")] string toDate)
    {
        Dictionary<string, object>? arguments = context.Arguments;
        string? from = arguments?.ContainsKey("from") == true ? arguments["from"]?.ToString() : fromDate;
        string? to = arguments?.ContainsKey("to") == true ? arguments["to"]?.ToString() : toDate;

        return await GetEstimatedIncomeAsync(from, to);
    }

    #endregion

    #region HTTP Equivalent Trigger User Functions (Now calling the async methods)

    //[Function(nameof(GetUserProfileHttp))]
    //public async Task<IActionResult> GetUserProfileHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/user/profile")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetUserProfile called");
    //    var result = await toshlAgent.User.GetProfileAsync();
    //    return new OkObjectResult(result);
    //}

    //[Function(nameof(GetAccountSummaryHttp))]
    //public async Task<IActionResult> GetAccountSummaryHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/user/summary")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetAccountSummary called");
    //    string? from = req.Query["from"];
    //    string? to = req.Query["to"];
    //    var result = await toshlAgent.User.GetAccountSummaryAsync(from, to);
    //    return new OkObjectResult(result);
    //}

    //[Function(nameof(GetPaymentTypesHttp))]
    //public async Task<IActionResult> GetPaymentTypesHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/user/paymenttypes")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetPaymentTypes called");
    //    var result = await toshlAgent.User.GetPaymentTypesAsync();
    //    return new OkObjectResult(result);
    //}

    //[Function(nameof(GetPaymentsHttp))]
    //public async Task<IActionResult> GetPaymentsHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/user/payments")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetPayments called");
    //    var result = await toshlAgent.User.GetPaymentsAsync();
    //    return new OkObjectResult(result);
    //}

    //[Function(nameof(GetBankConnectionsHttp))]
    //public async Task<IActionResult> GetBankConnectionsHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/bankconnections")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetBankConnections called");
    //    var connections = await toshlAgent.BankConnections.GetAllAsync();
    //    return new OkObjectResult(connections);
    //}

    //[Function(nameof(GetTransactionsHttp))]
    //public async Task<IActionResult> GetTransactionsHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/transactions")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetTransactions called");
    //    string? from = req.Query["from"];
    //    string? to = req.Query["to"];
    //    var transactions = await toshlAgent.Transactions.GetRangeAsync(from, to);
    //    return new OkObjectResult(transactions);
    //}

    //[Function(nameof(GetBudgetsHttp))]
    //public async Task<IActionResult> GetBudgetsHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/budgets")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetBudgets called");
    //    string? from = req.Query["from"];
    //    string? to = req.Query["to"];
    //    var budgets = await toshlAgent.Budgets.GetBudgetsAsync(from, to);
    //    return new OkObjectResult(budgets);
    //}

    //[Function(nameof(GetPlanningHttp))]
    //public async Task<IActionResult> GetPlanningHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/planning")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetPlanning called");
    //    string? from = req.Query["from"];
    //    string? to = req.Query["to"];
    //    var planning = await toshlAgent.Planning.ForecastAsync(from, to);
    //    return new OkObjectResult(planning);
    //}

    //[Function(nameof(GetCategoriesHttp))]
    //public async Task<IActionResult> GetCategoriesHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/categories")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetCategories called");
    //    var categories = await toshlAgent.Categories.GetCategoriesAsync();
    //    return new OkObjectResult(categories);
    //}

    //[Function(nameof(GetCategoryTagsHttp))]
    //public async Task<IActionResult> GetCategoryTagsHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/categories/tags")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetCategoryTags called");
    //    var tags = await toshlAgent.Categories.GetTagsAsync();
    //    return new OkObjectResult(tags);
    //}

    //[Function(nameof(GetCurrenciesHttp))]
    //public async Task<IActionResult> GetCurrenciesHttp(
    //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "toshl/currencies")]
    //    HttpRequestData req)
    //{
    //    logger.LogInformation("HTTP trigger for GetCurrencies called");
    //    var currencies = await toshlAgent.Currency.GetSupportedCurrenciesAsync();
    //    return new OkObjectResult(currencies);
    //}

    #endregion
}