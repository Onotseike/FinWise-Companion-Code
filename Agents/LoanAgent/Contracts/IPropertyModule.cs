using FinWise.LoanAgent.Models;
using FinWise.Shared.Core.Models;

namespace FinWise.LoanAgent.Contracts;

/// <summary>
/// Interface for property data retrieval that supports multiple countries
/// </summary>
public interface IPropertyModule
{
    /// <summary>
    /// Gets all property data for the specified country
    /// </summary>
    /// <param name="country">The supported country to get data for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An enumerable collection of property records for the specified country</returns>
    Task<IEnumerable<PropertyPriceBaseRecord>> GetAllAsync(SupportedCountry country, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property data by location for the specified country
    /// </summary>
    /// <param name="country">The supported country to get data for</param>
    /// <param name="location">Location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An enumerable collection of property records for the specified location</returns>
    Task<IEnumerable<PropertyPriceBaseRecord>> GetByLocationAsync(SupportedCountry country, string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property data within a price range for the specified country
    /// </summary>
    /// <param name="country">The supported country to get data for</param>
    /// <param name="minPrice">Minimum price</param>
    /// <param name="maxPrice">Maximum price</param>
    /// <param name="location">Optional location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An enumerable collection of property records within the price range</returns>
    Task<IEnumerable<PropertyPriceBaseRecord>> GetByPriceRangeAsync(SupportedCountry country, decimal minPrice, decimal maxPrice, string? location = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property data with country-specific filters
    /// </summary>
    /// <param name="country">The supported country to get data for</param>
    /// <param name="filters">Dictionary of filters (e.g., "beds", "postcode", "propertyType")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An enumerable collection of property records matching the filters</returns>
    Task<IEnumerable<PropertyPriceBaseRecord>> GetFilteredAsync(SupportedCountry country, Dictionary<string, object> filters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of supported countries for this module
    /// </summary>
    IEnumerable<SupportedCountry> SupportedCountries { get; }
}