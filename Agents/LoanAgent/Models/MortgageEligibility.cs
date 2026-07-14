using System.Text.Json.Serialization;

using FinWise.Shared.Core.Contracts;

namespace FinWise.LoanAgent.Models;

public record MortgageEligibility(
    [property: JsonPropertyName("eligible")] bool Eligible,
    [property: JsonPropertyName("maxBorrowing")] decimal MaxBorrowing,
    [property: JsonPropertyName("recommendedPropertyPrice")] decimal RecommendedPropertyPrice,
    [property: JsonPropertyName("loanToValue")] decimal LoanToValue,
    [property: JsonPropertyName("monthlyPaymentEstimate")] decimal MonthlyPaymentEstimate,
    [property: JsonPropertyName("riskFactors")] List<string> RiskFactors,
    [property: JsonPropertyName("improvementSuggestions")] List<string> ImprovementSuggestions
);

public record PropertySearchResult(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("bedrooms")] int Bedrooms,
    [property: JsonPropertyName("bathrooms")] int Bathrooms,
    [property: JsonPropertyName("propertyType")] string PropertyType,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("images")] List<string> Images,
    [property: JsonPropertyName("agent")] string Agent
);

public record LenderRecommendation(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("maxLtv")] decimal MaxLtv,
    [property: JsonPropertyName("incomeMultiple")] decimal IncomeMultiple,
    [property: JsonPropertyName("minDeposit")] string MinDeposit,
    [property: JsonPropertyName("specialFeatures")] List<string> SpecialFeatures,
    [property: JsonPropertyName("estimatedRate")] string EstimatedRate,
    [property: JsonPropertyName("suitabilityScore")] decimal SuitabilityScore
);

public record ImprovementStrategy(
    [property: JsonPropertyName("immediateActions")] List<ImprovementAction> ImmediateActions,
    [property: JsonPropertyName("shortTerm")] List<ImprovementAction> ShortTerm,
    [property: JsonPropertyName("mediumTerm")] List<ImprovementAction> MediumTerm,
    [property: JsonPropertyName("financialTargets")] FinancialTargets FinancialTargets,
    [property: JsonPropertyName("timelineMonths")] int TimelineMonths
);

public record ImprovementAction(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("methods")] List<string> Methods,
    [property: JsonPropertyName("benefit")] string Benefit,
    [property: JsonPropertyName("timeline")] string Timeline
);

public record FinancialTargets(
    [property: JsonPropertyName("targetIncome")] decimal TargetIncome,
    [property: JsonPropertyName("targetSavings")] decimal TargetSavings,
    [property: JsonPropertyName("targetDebt")] decimal TargetDebt
);

public record StressTestResult(
    [property: JsonPropertyName("propertyPrice")] decimal PropertyPrice,
    [property: JsonPropertyName("loanAmount")] decimal LoanAmount,
    [property: JsonPropertyName("scenarios")] List<StressTestScenario> Scenarios
);

public record StressTestScenario(
    [property: JsonPropertyName("interestRate")] decimal InterestRate,
    [property: JsonPropertyName("monthlyPayment")] decimal MonthlyPayment,
    [property: JsonPropertyName("paymentToIncomeRatio")] decimal PaymentToIncomeRatio,
    [property: JsonPropertyName("remainingMonthlyIncome")] decimal RemainingMonthlyIncome,
    [property: JsonPropertyName("affordable")] bool Affordable,
    [property: JsonPropertyName("stressLevel")] string StressLevel
);

// MCP Tool Requests and Responses
public record AnalyzeEligibilityRequest(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("country")] string Country = "auto_detect"
);

public record SearchPropertiesRequest(
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("maxPrice")] decimal MaxPrice,
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("propertyType")] string PropertyType = "any"
);

public record GenerateImprovementPlanRequest(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("targetLoanAmount")] decimal TargetLoanAmount,
    [property: JsonPropertyName("timelineMonths")] int TimelineMonths = 12
);

public record GetLenderRecommendationsRequest(
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("userId")] string UserId
);

public record StressTestAffordabilityRequest(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("propertyPrice")] decimal PropertyPrice,
    [property: JsonPropertyName("scenarios")] List<decimal> Scenarios
)
{
    public StressTestAffordabilityRequest(string userId, decimal propertyPrice) 
        : this(userId, propertyPrice, [2.0m, 4.0m, 6.0m, 8.0m]) { }
}

public record EstateIntelAPIResonse<T>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    T Data,
    int Status,
    Dictionary<string, string> Headers,
    [property: JsonPropertyName("error")] object? Error = null) :ApiResponse<T>(Data, Status, Headers);

#region Country Property Models

[Serializable]
public record PropertyPriceBaseRecord(
    DateTime DateOfSale,
    decimal Price,
    string County
);

[Serializable]
public record UKPropertyPriceRecord(
    string TransactionId,
    string Postcode,
    char PropertyType,
    char OldNew,
    char Duration,
    string PAON,
    string SAON,
    string Street,
    string Locality,
    string TownCity,
    string District,
    string CategoryType,
    string RecordStatus,
    DateTime DateOfSale,
    decimal Price,
    string County
) : PropertyPriceBaseRecord(DateOfSale, Price, County);

[Serializable]
public record IEPropertyPriceRecord(
    string Address,
    string Eircode,
    bool NotFullMarketPrice,
    bool VATExclusive,
    string DescriptionOfProperty,
    string PropertySizeDescription,
    DateTime DateOfSale,
    decimal Price,
    string County
) : PropertyPriceBaseRecord(DateOfSale, Price, County);

[Serializable]
public record NGPropertyPriceRecord(
    [property: JsonPropertyName("beds")] string Beds,
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("country_code")] string CountryCode,
    [property: JsonPropertyName("currency")] string Currency,
    DateTime DateOfSale,
    decimal Price,
    string County
) : PropertyPriceBaseRecord(DateOfSale, Price, County);

#endregion
