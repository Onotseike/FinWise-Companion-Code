using FinWise.BudgetingAgent.Plugins;
using FinWise.BudgetingAgent.ToshlApi.Contracts;
using FinWise.BudgetingAgent.ToshlApi.Modules;
using FinWise.Shared.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinWise.BudgetingAgent.ToshlApi.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddToshlApiClient(
        this IServiceCollection services,
        ApiClientConfig config,
        AuthConfig authConfig)
    {
        _ = services.AddMemoryCache();
        _ = services.AddHttpClient<IToshlApiClient, ToshlApiClient>();

        _ = services.AddSingleton(config);
        _ = services.AddSingleton(authConfig);
        _ = services.AddSingleton<IAuthProvider, AuthProvider>();
        _ = services.AddScoped<IToshlApiClient, ToshlApiClient>();
        _ = services.AddScoped<IUserProfileModule, UserProfileModule>();
        _ = services.AddScoped<ITransactionsModule, TransactionsModule>();
        _ = services.AddScoped<IBankConnectionsModule, BankConnectionsModule>();
        _ = services.AddScoped<IBudgetsModule, BudgetsModule>();
        _ = services.AddScoped<IPlanningModule, PlanningModule>();
        _ = services.AddScoped<ICurrencyModule, CurrencyModule>();
        _ = services.AddScoped<ICategoriesModule, CategoriesModule>();
        _ = services.AddScoped<IToshlAgent, ToshlAgent>();
        _ = services.AddScoped<ToshlPlugin>();

        return services;
    }

    public static IServiceCollection AddToshlApiClientFromEnvironment(this IServiceCollection services, IConfiguration configuration)
    {
        string baseUrl = configuration["Values:TOSHL_API_BASE_URL"] ?? throw new ArgumentNullException("TOSHL_API_BASE_URL");
        string token = configuration["Values:TOSHL_API_TOKEN"] ?? throw new ArgumentNullException("TOSHL_API_TOKEN");
        int timeoutSeconds = int.Parse(configuration["Values:API_TIMEOUT"] ?? "10");
        ApiClientConfig config = new(baseUrl, token, TimeSpan.FromSeconds(timeoutSeconds));
        AuthConfig authConfig = new(AuthType.Basic, token);
        (Uri endpoint, string id, TimeSpan frequency, string? instruction, string? description) = (new Uri(configuration["Values:MANAGEMENT_ENDPOINT"] ?? "http://localhost:7071"),
                    configuration["Values:AGENT_ID"] ?? "toshl-agent",
                    TimeSpan.FromSeconds(30),
                    """
                    You are a personal financial advisor AI assistant. You have access to the user's financial data through Toshl API functions.

                    Available functions:
                    - get_user_profile: Get user profile information
                    - get_account_summary: Get account summary with optional date range
                    - get_transactions: Get transactions within a date range
                    - get_budgets: Get all budgets with optional date range
                    - get_categories: Get all expense categories
                    - get_planning: Get financial planning forecast
                    - get_bank_connections: Get all bank connections
                    - get_currencies: Get all supported currencies
                    - get_payment_types: Get available payment types
                    - get_payments: Get user payments
                    - get_category_tags: Get all category tags

                    Analyze the user's financial situation and provide helpful insights, recommendations, and answers to their questions.
                    Always be helpful, accurate, and provide actionable advice. When discussing financial data, be specific and reference 
                    actual numbers from the user's accounts when available.

                    If you need more information to provide a complete answer, use the available functions to gather the necessary data.
                    """,
                "Toshl Agent, connects to you toshl account ot give detailed anaylisis of your spending");
        AgentOptions agentOptions = new(endpoint, id, frequency, instruction, description);
        _ = services.AddSingleton(agentOptions);

        return services.AddToshlApiClient(config, authConfig);
    }
}
