using FinWise.BudgetingAgent.ToshlApi.Contracts;
using FinWise.BudgetingAgent.ToshlApi.Modules.Helpers;
using FinWise.Shared.Core.Contracts;

using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent.ToshlApi.Modules;

public class UserProfileModule : IUserProfileModule
{
    private readonly IToshlApiClient _apiClient;
    private readonly ILogger<UserProfileModule> _logger;

    public UserProfileModule(IToshlApiClient apiClient, ILogger<UserProfileModule> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _logger.LogDebug("Me client initialized");
    }

    public async Task<User> GetProfileAsync()
    {
        _logger.LogDebug("Fetching user profile");
        ApiResponse<User> response = await _apiClient.GetAsync<User>("/me");
        if (response.Status == 200)
        {
            _logger.LogInformation("User profile fetched successfully");
            return response.Data;
        }
        else
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError("Failed to fetch user profile: {Error}", response.Data);
            }
            throw new Exception($"Failed to fetch user profile: {response.Data}");
        }
    }

    public async Task<Summary> GetAccountSummaryAsync(string? from = null, string? to = null)
    {
        (string normalizedFrom, string normalizedTo) = ModuleHelpers.NormalizeDateRange(from, to);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Fetching account summary from {From} to {To}", normalizedFrom, normalizedTo);
        }

        var parameters = new Dictionary<string, string>
        {
            ["from"] = normalizedFrom,
            ["to"] = normalizedTo
        };

        ApiResponse<Summary> response = await _apiClient.GetAsync<Summary>("/me/summary", parameters);

        if (response.Status == 200)
        {
            _logger.LogInformation("Account summary fetched successfully");
            return response.Data;
        }
        else
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError("Failed to fetch account summary: {Error}", response.Data);
            }
            throw new Exception($"Failed to fetch account summary: {response.Data}");
        }
    }

    public async Task<PaymentType[]> GetPaymentTypesAsync()
    {
        _logger.LogDebug("Fetching payment types");
        ApiResponse<PaymentType[]> response = await _apiClient.GetAsync<PaymentType[]>("/me/payments/types");
        if (response.Status == 200)
        {
            _logger.LogInformation("Payment types fetched successfully");
            return response.Data;
        }
        else
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError("Failed to fetch payment types: {Error}", response.Data);
            }
            throw new Exception($"Failed to fetch payment types: {response.Data}");
        }
    }

    public async Task<Payment[]> GetPaymentsAsync()
    {
        _logger.LogDebug("Fetching payments");
        ApiResponse<Payment[]> response = await _apiClient.GetAsync<Payment[]>("/me/payments");
        if (response.Status == 200)
        {
            _logger.LogInformation("Payments fetched successfully");
            return response.Data;
        }
        else
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError("Failed to fetch payments: {Error}", response.Data);
            }
            throw new Exception($"Failed to fetch payments: {response.Data}");
        }
    }
}
