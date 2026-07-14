using FinWise.LoanAgent.Contracts;
using FinWise.LoanAgent.Models;
using FinWise.LoanAgent.Services.APIClients;
using FinWise.Shared.Core.Contracts;
using FinWise.Shared.Core.Models;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FinWise.LoanAgent.Modules;

internal class PropertyModule : IPropertyModule
{
    private readonly CsvClient _csvClient;
    private readonly ILogger<PropertyModule> _logger;
    private readonly IMemoryCache _memoryCache;

    // Cache keys for different countries
    private const string UK_CACHE_KEY = "property_data_uk";
    private const string IE_CACHE_KEY = "property_data_ie";
    private const string NG_CACHE_KEY = "property_data_ng";

    // Cache expiration times
    private static readonly TimeSpan CsvCacheExpiration = TimeSpan.FromHours(24); // CSV data changes less frequently

    public IEnumerable<SupportedCountry> SupportedCountries => Enum.GetValues<SupportedCountry>();

    public PropertyModule(
        CsvClient csvClient,
        IMemoryCache memoryCache,
        ILogger<PropertyModule> logger)
    {
        _csvClient = csvClient ?? throw new ArgumentNullException(nameof(csvClient));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        logger.LogDebug("Property module initialized with CsvClient and MemoryCache");
    }

    public async Task<IEnumerable<PropertyPriceBaseRecord>> GetAllAsync(SupportedCountry country, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Getting all property data for country: {Country}", country);
        }

        try
        {
            return country switch
            {
                SupportedCountry.Nigeria => await GetNigerianPropertiesAsync(cancellationToken),
                SupportedCountry.Ireland => await GetIrishPropertiesAsync(cancellationToken),
                SupportedCountry.UnitedKingdom => await GetUKPropertiesAsync(cancellationToken), // Assuming UnitedKingdom uses UK data structure
                _ => throw new NotSupportedException($"Country {country} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get property data for country: {Country}", country);
            return [];
        }
    }

    public async Task<IEnumerable<PropertyPriceBaseRecord>> GetByLocationAsync(SupportedCountry country, string location, CancellationToken cancellationToken = default)
    {
        IEnumerable<PropertyPriceBaseRecord> allProperties = await GetAllAsync(country, cancellationToken);

        return country switch
        {
            SupportedCountry.Nigeria => allProperties.OfType<NGPropertyPriceRecord>()
                .Where(p => p.Location.Contains(location, StringComparison.OrdinalIgnoreCase))
                .Cast<PropertyPriceBaseRecord>(),

            SupportedCountry.Ireland => allProperties.OfType<IEPropertyPriceRecord>()
                .Where(p => p.Address.Contains(location, StringComparison.OrdinalIgnoreCase) ||
                           p.County.Contains(location, StringComparison.OrdinalIgnoreCase))
                .Cast<PropertyPriceBaseRecord>(),

            SupportedCountry.UnitedKingdom => allProperties.OfType<UKPropertyPriceRecord>()
                .Where(p => p.TownCity.Contains(location, StringComparison.OrdinalIgnoreCase) ||
                           p.County.Contains(location, StringComparison.OrdinalIgnoreCase))
                .Cast<PropertyPriceBaseRecord>(),

            _ => []
        };
    }

    public async Task<IEnumerable<PropertyPriceBaseRecord>> GetByPriceRangeAsync(SupportedCountry country, decimal minPrice, decimal maxPrice, string? location = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<PropertyPriceBaseRecord> allProperties = await GetAllAsync(country, cancellationToken);
        IEnumerable<PropertyPriceBaseRecord> filteredProperties = allProperties.Where(p => p.Price >= minPrice && p.Price <= maxPrice);

        if (!string.IsNullOrEmpty(location))
        {
            filteredProperties = await GetByLocationAsync(country, location, cancellationToken);
            filteredProperties = filteredProperties.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
        }

        return filteredProperties;
    }

    public async Task<IEnumerable<PropertyPriceBaseRecord>> GetFilteredAsync(SupportedCountry country, Dictionary<string, object> filters, CancellationToken cancellationToken = default)
    {
        IEnumerable<PropertyPriceBaseRecord> allProperties = await GetAllAsync(country, cancellationToken);

        foreach (KeyValuePair<string, object> filter in filters)
        {
            allProperties = ApplyFilter(allProperties, filter.Key, filter.Value, country);
        }

        return allProperties;
    }

    #region Private Helper Methods

    private async Task<IEnumerable<PropertyPriceBaseRecord>> GetNigerianPropertiesAsync(CancellationToken cancellationToken)
    {
        // Check cache first
        if (_memoryCache.TryGetValue(NG_CACHE_KEY, out IEnumerable<NGPropertyPriceRecord>? cachedData))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Retrieved Nigerian property data from cache ({Count} records)", cachedData?.Count() ?? 0);
            }
            return cachedData?.Cast<PropertyPriceBaseRecord>() ?? [];
        }

        _logger.LogDebug("Fetching Nigerian property data from CSV");

        ApiResponse<List<NGPropertyPriceRecord>> response = await _csvClient.GetAsync<List<NGPropertyPriceRecord>>("NG/nigeria_properties.csv", null);

        if (response.Status == 200 && response.Data != null)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Successfully retrieved {Count} Nigerian property records", response.Data.Count);
            }

            // Cache the data
            MemoryCacheEntryOptions cacheOptions = new()
            {
                AbsoluteExpirationRelativeToNow = CsvCacheExpiration,
                Priority = CacheItemPriority.Normal
            };

            _ = _memoryCache.Set(NG_CACHE_KEY, response.Data, cacheOptions);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Cached Nigerian property data for {Duration}", CsvCacheExpiration);
            }

            return response.Data.Cast<PropertyPriceBaseRecord>();
        }

        _logger.LogWarning("Failed to retrieve Nigerian property data from CSV");
        return [];
    }

    private async Task<IEnumerable<PropertyPriceBaseRecord>> GetIrishPropertiesAsync(CancellationToken cancellationToken)
    {
        // Check cache first
        if (_memoryCache.TryGetValue(IE_CACHE_KEY, out IEnumerable<IEPropertyPriceRecord>? cachedData))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Retrieved Irish property data from cache ({Count} records)", cachedData?.Count() ?? 0);
            }
            return cachedData?.Cast<PropertyPriceBaseRecord>() ?? [];
        }

        _logger.LogDebug("Fetching Irish property data from CSV");

        ApiResponse<List<IEPropertyPriceRecord>> response = await _csvClient.GetAsync<List<IEPropertyPriceRecord>>("IE/irish_properties.csv", null);

        if (response.Status == 200 && response.Data != null)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Successfully retrieved {Count} Irish property records", response.Data.Count);
            }

            // Cache the data
            MemoryCacheEntryOptions cacheOptions = new()
            {
                AbsoluteExpirationRelativeToNow = CsvCacheExpiration,
                Priority = CacheItemPriority.High // CSV data is expensive to read
            };

            _ = _memoryCache.Set(IE_CACHE_KEY, response.Data, cacheOptions);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Cached Irish property data for {Duration}", CsvCacheExpiration);
            }

            return response.Data.Cast<PropertyPriceBaseRecord>();
        }

        _logger.LogWarning("Failed to retrieve Irish property data. Status: {Status}", response.Status);
        return [];
    }

    private async Task<IEnumerable<PropertyPriceBaseRecord>> GetUKPropertiesAsync(CancellationToken cancellationToken)
    {
        // Check cache first
        if (_memoryCache.TryGetValue(UK_CACHE_KEY, out IEnumerable<UKPropertyPriceRecord>? cachedData))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Retrieved UK property data from cache ({Count} records)", cachedData?.Count() ?? 0);
            }
            return cachedData?.Cast<PropertyPriceBaseRecord>() ?? [];
        }

        _logger.LogDebug("Fetching UK property data from CSV");

        ApiResponse<List<UKPropertyPriceRecord>> response = await _csvClient.GetAsync<List<UKPropertyPriceRecord>>("UK/uk_properties.csv", null);

        if (response.Status == 200 && response.Data != null)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Successfully retrieved {Count} UK property records", response.Data.Count);
            }

            // Cache the data
            MemoryCacheEntryOptions cacheOptions = new()
            {
                AbsoluteExpirationRelativeToNow = CsvCacheExpiration,
                Priority = CacheItemPriority.High // CSV data is expensive to read
            };

            _ = _memoryCache.Set(UK_CACHE_KEY, response.Data, cacheOptions);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Cached UK property data for {Duration}", CsvCacheExpiration);
            }

            return response.Data.Cast<PropertyPriceBaseRecord>();
        }

        _logger.LogWarning("Failed to retrieve UK property data. Status: {Status}", response.Status);
        return [];
    }

    private static IEnumerable<PropertyPriceBaseRecord> ApplyFilter(IEnumerable<PropertyPriceBaseRecord> properties, string filterKey, object filterValue, SupportedCountry country) => filterKey.ToLowerInvariant() switch
    {
        "beds" when country == SupportedCountry.Nigeria =>
            properties.OfType<NGPropertyPriceRecord>()
                .Where(p => p.Beds.Equals(filterValue.ToString(), StringComparison.OrdinalIgnoreCase))
                .Cast<PropertyPriceBaseRecord>(),

        "postcode" when country == SupportedCountry.UnitedKingdom =>
            properties.OfType<UKPropertyPriceRecord>()
                .Where(p => p.Postcode.Contains(filterValue.ToString() ?? "", StringComparison.OrdinalIgnoreCase))
                .Cast<PropertyPriceBaseRecord>(),

        "eircode" when country == SupportedCountry.Ireland =>
            properties.OfType<IEPropertyPriceRecord>()
                .Where(p => p.Eircode.Contains(filterValue.ToString() ?? "", StringComparison.OrdinalIgnoreCase))
                .Cast<PropertyPriceBaseRecord>(),

        "minprice" when decimal.TryParse(filterValue.ToString(), out decimal minPrice) =>
            properties.Where(p => p.Price >= minPrice),

        "maxprice" when decimal.TryParse(filterValue.ToString(), out decimal maxPrice) =>
            properties.Where(p => p.Price <= maxPrice),

        _ => properties
    };

    /// <summary>
    /// Clears the cache for a specific country
    /// </summary>
    public void ClearCache(SupportedCountry country)
    {
        string cacheKey = country switch
        {
            SupportedCountry.Nigeria => NG_CACHE_KEY,
            SupportedCountry.Ireland => IE_CACHE_KEY,
            SupportedCountry.UnitedKingdom => UK_CACHE_KEY,
            _ => throw new NotSupportedException($"Country {country} is not supported")
        };

        _memoryCache.Remove(cacheKey);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Cleared cache for country: {Country}", country);
        }
    }

    /// <summary>
    /// Clears all cached property data
    /// </summary>
    public void ClearAllCache()
    {
        _memoryCache.Remove(NG_CACHE_KEY);
        _memoryCache.Remove(IE_CACHE_KEY);
        _memoryCache.Remove(UK_CACHE_KEY);
        _logger.LogDebug("Cleared all property data cache");
    }

    #endregion
}
