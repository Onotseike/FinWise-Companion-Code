using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using FinWise.Shared.Core.Configuration;
using FinWise.Shared.Core;
using FinWise.SupervisorAgent.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using FinWise.Shared.Core.AgentFramework;
using Microsoft.Agents.AI;
using Microsoft.Azure.Functions.Worker;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Load agent configuration
        var agentConfig = new AgentConfiguration();
        config.GetSection("AgentConfiguration").Bind(agentConfig);
        _ = services.AddSingleton(config);
        _ = services.AddSingleton(agentConfig);

        // Logging
        _ = services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Register Supervisor Agent Options
        _ = services.AddSingleton<AgentOptions>(sp =>
        {
            var managementEndpoint = new Uri(config["SUPERVISOR_MANAGEMENT_ENDPOINT"] ?? "http://localhost:7070");
            return new AgentOptions(
                managementEndPoint: managementEndpoint,
                id: "supervisor",
                heartBeatFrequency: TimeSpan.FromSeconds(30),
                instructions: "You are the Supervisor Agent for FinWise. Route requests to appropriate specialist agents.",
                description: "Multi-agent orchestration supervisor"
            );
        });

        // Create AIAgent (null for Supervisor since it routes to other agents)
        _ = services.AddSingleton<AIAgent>(sp =>
        {
            var options = sp.GetRequiredService<AgentOptions>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return AgentFactory.CreateAgent(config, options, loggerFactory: loggerFactory);
        });

        // Register SupervisorFinancialsAIAgent (wraps AIAgent with retry logic, token tracking, and metrics)
        _ = services.AddSingleton<SupervisorFinancialsAIAgent>();

        // Register SupervisorAgentOrchestrator (null AIAgent - routes to other agents)
        _ = services.AddScoped(sp =>
        {
            var options = sp.GetRequiredService<AgentOptions>();
            var supervisorFinancialsAIAgent = sp.GetRequiredService<SupervisorFinancialsAIAgent>();
            var sbClient = sp.GetRequiredService<ServiceBusClient>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<SupervisorAgentOrchestrator>();
            return new SupervisorAgentOrchestrator(null, options, supervisorFinancialsAIAgent, sbClient, logger, sp);
        });

        // Register ServiceBusClient for delegating requests to specialist agents
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

        })
    .Build();

host.Run();
