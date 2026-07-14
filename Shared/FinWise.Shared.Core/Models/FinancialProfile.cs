using System.Text.Json.Serialization;

namespace FinWise.Shared.Core.Models;

/// <summary>
/// Represents a user's complete financial profile for analysis.
/// </summary>
public record FinancialProfile(
    [property: JsonPropertyName("annualIncome")] decimal AnnualIncome,
    [property: JsonPropertyName("monthlyExpenses")] decimal MonthlyExpenses,
    [property: JsonPropertyName("existingDebts")] decimal ExistingDebts,
    [property: JsonPropertyName("savings")] decimal Savings,
    [property: JsonPropertyName("age")] int Age,
    [property: JsonPropertyName("dependents")] int Dependents,
    [property: JsonPropertyName("country")] SupportedCountry Country,
    [property: JsonPropertyName("employmentType")] string EmploymentType = "permanent",
    [property: JsonPropertyName("creditScore")] int? CreditScore = null
);
