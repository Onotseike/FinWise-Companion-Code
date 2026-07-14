using System.Text.Json;

using FinWise.Shared.Core;
using FinWise.Shared.Core.Contracts;

using Microsoft.Extensions.Caching.Memory;

using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent.ToshlApi;

public interface IToshlApiClient
{
    Task<ApiResponse<T>> GetAsync<T>(string path, Dictionary<string, string>? parameters = null);
    Task<ApiResponse<T>> PostAsync<T>(string path, object? data = null, Dictionary<string, string>? parameters = null);
    Task<ApiResponse<T>> PutAsync<T>(string path, object? data = null, Dictionary<string, string>? parameters = null);
    Task<ApiResponse<T>> DeleteAsync<T>(string path, Dictionary<string, string>? parameters = null);
    bool IsAuthenticated();
    string GetBaseUrl();
}

public class ToshlApiClient : IToshlApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthProvider _authProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ToshlApiClient> _logger;
    private readonly string _baseUrl;

    public ToshlApiClient(
        HttpClient httpClient,
        IAuthProvider authProvider,
        IMemoryCache cache,
        ILogger<ToshlApiClient> logger,
        ApiClientConfig config)
    {
        _httpClient = httpClient;
        _authProvider = authProvider;
        _cache = cache;
        _logger = logger;
        _baseUrl = config.BaseUrl;

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(config.BaseUrl);
        _httpClient.Timeout = config.Timeout ?? TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Toshl API client initialized with base URL: {BaseUrl}", config.BaseUrl);
        }
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string path, Dictionary<string, string>? parameters = null) => await SendRequestAsync<T>(HttpMethod.Get, path, null, parameters);

    public async Task<ApiResponse<T>> PostAsync<T>(string path, object? data = null, Dictionary<string, string>? parameters = null) => await SendRequestAsync<T>(HttpMethod.Post, path, data, parameters);

    public async Task<ApiResponse<T>> PutAsync<T>(string path, object? data = null, Dictionary<string, string>? parameters = null) => await SendRequestAsync<T>(HttpMethod.Put, path, data, parameters);

    public async Task<ApiResponse<T>> DeleteAsync<T>(string path, Dictionary<string, string>? parameters = null) => await SendRequestAsync<T>(HttpMethod.Delete, path, null, parameters);

    private async Task<ApiResponse<T>> SendRequestAsync<T>(
        HttpMethod method,
        string path,
        object? data = null,
        Dictionary<string, string>? parameters = null)
    {
        try
        {
            using HttpRequestMessage request = new(method, BuildUrl(path, parameters));

            // Add authentication headers
            Dictionary<string, string> authHeaders = _authProvider.GetAuthHeaders();
            foreach (KeyValuePair<string, string> header in authHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            // Add caching headers
            string cacheKey = $"etag:{method}:{path}";
            if (_cache.TryGetValue(cacheKey, out string? etag))
            {
                request.Headers.Add("If-None-Match", etag);
            }

            // Add request body for POST/PUT
            if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                string json = JsonSerializer.Serialize(data, JsonSerializationDefaults.SnakeCaseApi);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            // Handle 304 Not Modified
            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
            {
                string dataCacheKey = $"data:{method}:{path}";
                if (_cache.TryGetValue(dataCacheKey, out T? cachedData))
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Using cached data (304 Not Modified) for URL: {Url}", path);
                    }
                    return new ApiResponse<T>(cachedData!, 200, CollectionDefaults.EmptyStringDictionary);
                }
            }

            _ = response.EnsureSuccessStatusCode();

            // Cache ETag if present
            if (response.Headers.ETag?.Tag != null)
            {
                _ = _cache.Set(cacheKey, response.Headers.ETag.Tag, TimeSpan.FromMinutes(30));
            }

            string content = await response.Content.ReadAsStringAsync();
            T? result = JsonSerializer.Deserialize<T>(content, JsonSerializationDefaults.SnakeCaseApi);

            // Cache response data
            if (response.Headers.ETag?.Tag != null && result != null)
            {
                string dataCacheKey = $"data:{method}:{path}";
                _ = _cache.Set(dataCacheKey, result, TimeSpan.FromMinutes(30));
            }

            Dictionary<string, string> headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));

            return new ApiResponse<T>(result!, (int)response.StatusCode, headers);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for {Method} {Path}", method, path);
            throw new ToshlApiException($"API request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed for {Method} {Path}", method, path);
            throw new ToshlApiException($"Failed to parse API response: {ex.Message}", ex);
        }
    }

    private static string BuildUrl(string path, Dictionary<string, string>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return path;

        string query = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        return $"{path}?{query}";
    }

    public bool IsAuthenticated() => _authProvider.IsConfigured();

    public string GetBaseUrl() => _baseUrl;
}

public class ToshlApiException : Exception
{
    public ToshlApiException(string message) : base(message) { }
    public ToshlApiException(string message, Exception innerException) : base(message, innerException) { }
}