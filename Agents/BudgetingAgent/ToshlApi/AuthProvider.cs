using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent.ToshlApi;

public enum AuthType
{
    Basic,
    OAuth
}

public record AuthConfig(AuthType Type, string? Token = null, string? Username = null, string? Password = null);

public interface IAuthProvider
{
    Dictionary<string, string> GetAuthHeaders();
    bool IsConfigured();
}

public class AuthProvider : IAuthProvider
{
    private readonly AuthConfig _config;
    private readonly ILogger<AuthProvider> _logger;

    public AuthProvider(AuthConfig config, ILogger<AuthProvider> logger)
    {
        _config = config;
        _logger = logger;
        _logger.LogDebug("Auth provider initialized with type: {AuthType}", config.Type);
    }

    public Dictionary<string, string> GetAuthHeaders() => _config.Type switch
    {
        AuthType.Basic when !string.IsNullOrEmpty(_config.Token) => new()
        {
            ["Authorization"] = $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_config.Token}:"))}"
        },
        AuthType.Basic => throw new InvalidOperationException("Missing token for basic authentication"),
        AuthType.OAuth when !string.IsNullOrEmpty(_config.Token) => new()
        {
            ["Authorization"] = $"Bearer {_config.Token}"
        },
        AuthType.OAuth => throw new InvalidOperationException("Missing token for OAuth authentication"),
        _ => throw new NotSupportedException($"Authentication type {_config.Type} is not supported")
    };

    public bool IsConfigured() => _config.Type switch
    {
        AuthType.Basic => !string.IsNullOrEmpty(_config.Token),
        AuthType.OAuth => !string.IsNullOrEmpty(_config.Token),
        _ => false
    };
}