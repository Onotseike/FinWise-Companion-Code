using System.Net;
using System.Text.Json;

using FinWise.Shared.Core;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent;

public class McpServerFunctions(ILogger<McpServerFunctions> logger, IConfiguration configuration)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [Function("GetTools")]
    public Task<string> GetTools(
        [McpToolTrigger("List Tools", "Lists all tools available to this agent.")]
        ToolInvocationContext context)
    {
        logger.LogInformation("Listing available MCP tools");

        object[] tools =
        [            
            // User Profile Tools
            new
            {
                name = "getUserProfile",
                description = "Get the user profile information from Toshl",
                inputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = CollectionDefaults.EmptyStringArray
                }
            },
            new
            {
                name = "getAccountSummary",
                description = "Get account summary with optional date range",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        from = new { type = "string", description = "Start date (YYYY-MM-DD)" },
                        to = new { type = "string", description = "End date (YYYY-MM-DD)" }
                    },
                    required = CollectionDefaults.EmptyStringArray
                }
            },
            new
            {
                name = "getPaymentTypes",
                description = "Get available payment types",
                inputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = CollectionDefaults.EmptyStringArray
                }
            },
            new
            {
                name = "getPayments",
                description = "Get user payments",
                inputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = CollectionDefaults.EmptyStringArray
                }
            },
            // Bank Connections Tools
            new
            {
                name = "getBankConnections",
                description = "Get all bank connections",
                inputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = CollectionDefaults.EmptyStringArray
                }
            },
            // Transactions Tools
            new
            {
                name = "getTransactions",
                description = "Get transactions within a date range",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        from = new { type = "string", description = "Start date (YYYY-MM-DD)" },
                        to = new { type = "string", description = "End date (YYYY-MM-DD)" }
                    },
                    required = CollectionDefaults.EmptyStringArray
                }
            },
            // Budgets Tools
            new
            {
                name = "getBudgets",
                description = "Get all budgets",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        from = new { type = "string", description = "Start date (YYYY-MM-DD)" },
                        to = new { type = "string", description = "End date (YYYY-MM-DD)" }
                    },
                    required = CollectionDefaults.EmptyStringArray
                }
            },
            // Planning Tools
            new
            {
                name = "getPlanning",
                description = "Get financial planning forecast within a date range",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        from = new { type = "string", description = "Start date (YYYY-MM-DD)" },
                        to = new { type = "string", description = "End date (YYYY-MM-DD)" }
                    },
                    required = CollectionDefaults.EmptyStringArray
                }
            },
            // Categories Tools
            new
            {
                name = "getCategories",
                description = "Get all categories",
                inputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = CollectionDefaults.EmptyStringArray
                }
            },
            new
            {
                name = "getCategoryTags",
                description = "Get all category tags",
                inputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = CollectionDefaults.EmptyStringArray
                }
            },
            // Currencies Tools
            new
            {
                name = "getCurrencies",
                description = "Get all supported currencies",
                inputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = CollectionDefaults.EmptyStringArray
                }
            }
        ];

        return Task.FromResult(JsonSerializer.Serialize(new { tools }, s_jsonOptions));
    }

    [Function("GetServerInfo")]
    public async Task<HttpResponseData> GetServerInfo(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        logger.LogInformation("Getting MCP server information");

        var serverInfo = new
        {
            name = configuration["MCP_SERVER_NAME"] ?? "toshl-mcp-server",
            version = configuration["MCP_SERVER_VERSION"] ?? "1.0.0",
            description = "Toshl Finance MCP Server for Azure Functions",
            capabilities = new
            {
                tools = true,
                resources = false,
                prompts = false
            }
        };

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");

        await response.WriteStringAsync(JsonSerializer.Serialize(serverInfo, s_jsonOptions));
        return response;
    }
}
