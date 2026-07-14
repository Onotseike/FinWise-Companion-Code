using FinWise.Shared.Core.Contracts;

namespace FinWise.LoanAgent.Contracts;

public interface IApiClient
{
    Task<ApiResponse<T>> GetAsync<T>(string path, Dictionary<string, string>? parameters = null);
    Task<ApiResponse<T>> PostAsync<T>(string path, object? data = null, Dictionary<string, string>? parameters = null);
    Task<ApiResponse<T>> PutAsync<T>(string path, object? data = null, Dictionary<string, string>? parameters = null);
    Task<ApiResponse<T>> DeleteAsync<T>(string path, Dictionary<string, string>? parameters = null);
    bool IsAuthenticated();
    string GetBaseUrl();
}
