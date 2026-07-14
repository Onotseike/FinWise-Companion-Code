using Azure.Cosmos;
using Azure.Messaging.ServiceBus;

using FinWise.LoanAgent;
using FinWise.LoanAgent.Contracts;
using FinWise.LoanAgent.Modules;
using FinWise.LoanAgent.Services;
using FinWise.LoanAgent.Services.APIClients;
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

        // Agent identity and management
        var loanAgentOptions = new AgentOptions(
            managementEndPoint: new Uri(config["LOAN_MANAGEMENT_ENDPOINT"] ?? "http://localhost:7072"),
            id: "loan",
            heartBeatFrequency: TimeSpan.FromSeconds(30),
            instructions: "You are the Loan Agent for FinWise. Analyze mortgage options and provide lending advice.",
            description: "Loan and Mortgage Agent for property and lending analysis"
        );
        _ = services.AddSingleton(loanAgentOptions);

        _ = services.AddMemoryCache();
        CsvClient csvClient = new("HousingData");
        _ = services.AddSingleton(csvClient);
        _ = services.AddScoped<IPropertyModule, PropertyModule>();
        _ = services.AddScoped<IMortgageAgent, MortgageAgent>();

        // Create AIAgent with MortgagePlugin tools
        _ = services.AddScoped<MortgagePlugin>();
        var sp = services.BuildServiceProvider();
        var mortgagePlugin = sp.GetRequiredService<MortgagePlugin>();
        AIAgent loanAgent = AgentFactory.CreateAgent(
            config,
            loanAgentOptions,
            tools: mortgagePlugin.GetAllTools(),
            loggerFactory: services.BuildServiceProvider().GetRequiredService<ILoggerFactory>());

        _ = services.AddSingleton(loanAgent);
        // Register LoanFinancialsAIAgent (wraps AIAgent with retry logic, token tracking, and metrics)
        _ = services.AddSingleton<LoanFinancialsAIAgent>();

        // Service Bus client — required by LoanA2AFunctions (inbound A2A trigger)
        var sbConnection = config["ServiceBus:Connection"] ?? "";
        if (!string.IsNullOrEmpty(sbConnection) && sbConnection != "your-service-bus-connection-string-here")
        {
            _ = services.AddSingleton(new ServiceBusClient(sbConnection));
        }
        else
        {
            // Register a placeholder so DI doesn't throw during local startup without real credentials
            _ = services.AddSingleton<ServiceBusClient>(_ =>
                new ServiceBusClient("Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=placeholder="));
        }

        // Register Cosmos DB client for state persistence
        var cosmosConnectionString = config["COSMOS_CONNECTION_STRING"] ?? "";
        if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            var cosmosClient = new CosmosClient(cosmosConnectionString);
            _ = services.AddSingleton(cosmosClient);
            var dbName = config["COSMOS_DATABASE_NAME"] ?? "FinWiseDb";
            var containerName = config["COSMOS_LOAN_CONTAINER"] ?? "loan-state";
            _ = services.AddScoped(sp => new LoanAgentStateHandler(
                cosmosClient,
                dbName,
                containerName,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<LoanAgentStateHandler>()
            ));
        }

        // Register the modernized LoanAgentOrchestrator
        _ = services.AddScoped(sp =>
        {
            var aiAgent = sp.GetRequiredService<AIAgent>();
            var options = sp.GetRequiredService<AgentOptions>();
            var loanFinancialsAIAgent = sp.GetRequiredService<LoanFinancialsAIAgent>();
            var stateHandler = sp.GetService<LoanAgentStateHandler>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<LoanAgentOrchestrator>();
            return new LoanAgentOrchestrator(aiAgent, options, loanFinancialsAIAgent, stateHandler!, logger, sp);
        });
        })
    .Build();

host.Run();
