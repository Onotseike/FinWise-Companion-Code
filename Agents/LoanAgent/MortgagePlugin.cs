using System.ComponentModel;
using System.Text.Json;

using FinWise.LoanAgent.Contracts;
using FinWise.LoanAgent.Models;
using FinWise.Shared.Core.Models;

using Microsoft.Extensions.AI;

namespace FinWise.LoanAgent;

internal class MortgagePlugin(IMortgageAgent mortgageAgent)
{
    private readonly IMortgageAgent _mortgageAgent = mortgageAgent ?? throw new ArgumentNullException(nameof(mortgageAgent));
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [Description("Get all properties available for mortgage in a given country")]
    public async Task<string> GetPropertiesAsync(
        [Description("country to get properties for e.g: Nigeria, Ireland, UnitedKingdom")] string country)
    {
        if (!Enum.TryParse(country, true, out SupportedCountry supportedCountry) || !_mortgageAgent.Properties.SupportedCountries.Contains(supportedCountry))
        {
            return $"Error: Unsupported country '{country}'. Supported countries are: {string.Join(", ", _mortgageAgent.Properties.SupportedCountries)}";
        }
        IEnumerable<PropertyPriceBaseRecord> properties = await _mortgageAgent.Properties.GetAllAsync(supportedCountry);
        return JsonSerializer.Serialize(properties, s_jsonOptions);
    }

    [Description("Get properties available for mortgage in a given country and location")]
    public async Task<string> GetPropertiesByLocationAsync(
        [Description("country to get properties for e.g: Nigeria, nigeria, Ireland")] string country,
        [Description("location in the country")] string location)
    {
        if (!Enum.TryParse(country, true, out SupportedCountry supportedCountry) || !_mortgageAgent.Properties.SupportedCountries.Contains(supportedCountry))
        {
            return $"Error: Unsupported country '{country}'. Supported countries are: {string.Join(", ", _mortgageAgent.Properties.SupportedCountries)}";
        }
        if (string.IsNullOrWhiteSpace(location))
        {
            return "Error: Location must be provided.";
        }
        IEnumerable<PropertyPriceBaseRecord> properties = await _mortgageAgent.Properties.GetByLocationAsync(supportedCountry, location);
        return JsonSerializer.Serialize(properties, s_jsonOptions);
    }

    [Description("Get properties available for mortgage in a given country within a price range")]
    public async Task<string> GetPropertiesByPriceRangeAsync(
        [Description("country to get properties for e.g: Nigeria, Ireland, UnitedKingdom")] string country,
        [Description("minimum price")] decimal minPrice,
        [Description("maximum price")] decimal maxPrice,
        [Description("location in the country")] string? location = null)
    {
        if (!Enum.TryParse(country, true, out SupportedCountry supportedCountry) || !_mortgageAgent.Properties.SupportedCountries.Contains(supportedCountry))
        {
            return $"Error: Unsupported country '{country}'. Supported countries are: {string.Join(", ", _mortgageAgent.Properties.SupportedCountries)}";
        }
        if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
        {
            return "Error: Invalid price range.";
        }
        IEnumerable<PropertyPriceBaseRecord> properties = await _mortgageAgent.Properties.GetByPriceRangeAsync(supportedCountry, minPrice, maxPrice, location);
        return JsonSerializer.Serialize(properties, s_jsonOptions);
    }

    [Description("Get properties available for mortgage in a given country with specific filters")]
    public async Task<string> GetPropertiesWithFiltersAsync(
        [Description("country to get properties for e.g: Nigeria, Ireland, UnitedKingdom")] string country,
        [Description("filters in JSON format")] string filtersJson)
    {
        if (!Enum.TryParse(country, true, out SupportedCountry supportedCountry) || !_mortgageAgent.Properties.SupportedCountries.Contains(supportedCountry))
        {
            return $"Error: Unsupported country '{country}'. Supported countries are: {string.Join(", ", _mortgageAgent.Properties.SupportedCountries)}";
        }
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return "Error: Filters must be provided in JSON format.";
        }
        Dictionary<string, object> filters;
        try
        {
            filters = JsonSerializer.Deserialize<Dictionary<string, object>>(filtersJson) ?? [];
        }
        catch (JsonException)
        {
            return "Error: Invalid JSON format for filters.";
        }
        IEnumerable<PropertyPriceBaseRecord> properties = await _mortgageAgent.Properties.GetFilteredAsync(supportedCountry, filters);
        return JsonSerializer.Serialize(properties, s_jsonOptions);
    }

    public IList<AITool> GetAllTools() =>
    [
        AIFunctionFactory.Create(this.GetPropertiesAsync),
        AIFunctionFactory.Create(this.GetPropertiesByLocationAsync),
        AIFunctionFactory.Create(this.GetPropertiesByPriceRangeAsync),
        AIFunctionFactory.Create(this.GetPropertiesWithFiltersAsync)
    ];
}
