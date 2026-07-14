using FinWise.LoanAgent.Contracts;
using FinWise.LoanAgent.Models;
using FinWise.LoanAgent.Services.CSVReaders;
using FinWise.Shared.Core;
using FinWise.Shared.Core.Contracts;

namespace FinWise.LoanAgent.Services.APIClients;

public class CsvClient(string baseUrl) : IApiClient
{
    private readonly string _baseUrl = baseUrl;

    public string GetBaseUrl() => _baseUrl;

    public Task<ApiResponse<T>> GetAsync<T>(string path, Dictionary<string, string>? parameters)
    {
        try
        {
            string fullPath = Path.Combine(_baseUrl, path);
            if (typeof(T) == typeof(List<UKPropertyPriceRecord>))
            {
                UKPropertyPriceCsvReader reader = new();
                List<UKPropertyPriceRecord> records = [.. reader.ReadFromCsv(fullPath)];
                // Optionally filter records using parameters here
                return Task.FromResult(new ApiResponse<T>((T)(object)records, 200, CollectionDefaults.EmptyStringDictionary));
            }
            else if (typeof(T) == typeof(List<IEPropertyPriceRecord>))
            {
                IEPropertyPriceCsvReader reader = new();
                List<IEPropertyPriceRecord> records = [.. reader.ReadFromCsv(fullPath)];
                // Optionally filter records using parameters here
                return Task.FromResult(new ApiResponse<T>((T)(object)records, 200, CollectionDefaults.EmptyStringDictionary));
            }
            else if (typeof(T) == typeof(List<NGPropertyPriceRecord>))
            {
                NGPropertyPriceCsvReader reader = new();
                List<NGPropertyPriceRecord> records = [.. reader.ReadFromCsv(fullPath)];
                // Optionally filter records using parameters here
                return Task.FromResult(new ApiResponse<T>((T)(object)records, 200, CollectionDefaults.EmptyStringDictionary));
            }
            else
            {
                throw new NotSupportedException($"Type {typeof(T).Name} is not supported by CsvClient.");
            }

        }
        catch (Exception ex)
        {
            object emptyList = typeof(T) == typeof(List<UKPropertyPriceRecord>)
                ? new List<UKPropertyPriceRecord>()
                : typeof(T) == typeof(List<IEPropertyPriceRecord>) ? new List<IEPropertyPriceRecord>()
                : typeof(T) == typeof(List<NGPropertyPriceRecord>) ? new List<NGPropertyPriceRecord>() : default(T)!;
            return Task.FromResult(new ApiResponse<T>((T)emptyList, 500, new Dictionary<string, string>
            {
                { "error", ex.Message }
            }));
        }
    }

    public Task<ApiResponse<T>> PostAsync<T>(string path, object? data = null, Dictionary<string, string>? parameters = null) => throw new NotSupportedException("POST operations are not supported for CSV data sources.");

    public Task<ApiResponse<T>> PutAsync<T>(string path, object? data = null, Dictionary<string, string>? parameters = null) => throw new NotSupportedException("PUT operations are not supported for CSV data sources.");

    public Task<ApiResponse<T>> DeleteAsync<T>(string path, Dictionary<string, string>? parameters = null) => throw new NotSupportedException("DELETE operations are not supported for CSV data sources.");

    public bool IsAuthenticated() =>
        // CSV files don't require authentication
        true;
}
