using Azure.Cosmos;
using Azure.Messaging.ServiceBus;

using FinWise.BudgetingAgent.Plugins;
using FinWise.BudgetingAgent.Services;
using FinWise.BudgetingAgent.ToshlApi.Extensions;
using FinWise.Shared.Core;
using FinWise.Shared.Core.AgentFramework;

using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("local.settings.json", optional: true)
            .Build();

        _ = services.AddSingleton(config);

        // Logging
        _ = services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Toshl API client
        _ = services.AddToshlApiClientFromEnvironment(config);

        // Register AgentOptions for BudgetingAgent
        var budgetingAgentOptions = new AgentOptions(
            managementEndPoint: new Uri(config["BUDGETING_MANAGEMENT_ENDPOINT"] ?? "http://localhost:7071"),
            id: "budgeting",
            heartBeatFrequency: TimeSpan.FromSeconds(30),
            instructions: "You are the Budgeting Agent for FinWise. Analyze financial data and provide budget advice.",
            description: "Budgeting Agent for financial planning and analysis"
        );
        _ = services.AddSingleton(budgetingAgentOptions);

        // Build Toshl plugin
        var sp = services.BuildServiceProvider();
        var toshlPlugin = sp.GetRequiredService<ToshlPlugin>();

        // Create AIAgent with Toshl plugin tools
        var budgetAgent = AgentFactory.CreateAgent(
            config,
            budgetingAgentOptions,
            tools: toshlPlugin.GetAllTools(),
            loggerFactory: sp.GetRequiredService<ILoggerFactory>());
        _ = services.AddSingleton(budgetAgent);

        // Register Cosmos DB client for state persistence
        var cosmosConnectionString = config["COSMOS_CONNECTION_STRING"] ?? "";
        if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            var cosmosClient = new CosmosClient(cosmosConnectionString);
            _ = services.AddSingleton(cosmosClient);

            var dbName = config["COSMOS_DATABASE_NAME"] ?? "FinWiseDb";
            var containerName = config["COSMOS_BUDGETING_CONTAINER"] ?? "budgeting-state";
            _ = services.AddScoped(sp => new BudgetingAgentStateHandler(
                cosmosClient,
                dbName,
                containerName,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<BudgetingAgentStateHandler>()
            ));
        }

        // Register ServiceBusClient for A2A message publishing and receiving
        var sbConnection = config["ServiceBus:Connection"] ?? "";
        if (!string.IsNullOrEmpty(sbConnection) && sbConnection != "your-service-bus-connection-string-here")
        {
            _ = services.AddSingleton(new ServiceBusClient(sbConnection));
        }
        else
        {
            // Register a null-safe placeholder so DI doesn't throw on startup without real credentials
            _ = services.AddSingleton<ServiceBusClient>(_ =>
                new ServiceBusClient("Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=placeholder="));
        }

        // Register FinancialsAIAgent (wraps AIAgent with retry logic, token tracking, and metrics)
        _ = services.AddSingleton<FinancialsAIAgent>();

        // Register the modernized BudgetingAgentOrchestrator
        // Orchestrates domain logic, state persistence, and delegates AI invocation to FinancialsAIAgent
        _ = services.AddScoped(sp =>
        {
            var aiAgent = sp.GetRequiredService<AIAgent>();
            var options = sp.GetRequiredService<AgentOptions>();
            var financialsAIAgent = sp.GetRequiredService<FinancialsAIAgent>();
            var stateHandler = sp.GetService<BudgetingAgentStateHandler>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<BudgetingAgentOrchestrator>();
            return new BudgetingAgentOrchestrator(aiAgent, options, financialsAIAgent, stateHandler, logger, sp);
        });

        })
    .Build();

host.Run();
