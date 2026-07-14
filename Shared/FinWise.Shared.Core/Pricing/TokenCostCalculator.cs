namespace FinWise.Shared.Core.Pricing;

/// <summary>
/// Result of a cost calculation including breakdown and total cost.
/// </summary>
public class TokenCostResult
{
    /// <summary>Gets or sets the input tokens that were charged.</summary>
    public long InputTokens { get; set; }

    /// <summary>Gets or sets the output tokens that were charged.</summary>
    public long OutputTokens { get; set; }

    /// <summary>Gets or sets the total tokens (input + output).</summary>
    public long TotalTokens => InputTokens + OutputTokens;

    /// <summary>Gets or sets the cost of input tokens in USD.</summary>
    public decimal InputCostUsd { get; set; }

    /// <summary>Gets or sets the cost of output tokens in USD.</summary>
    public decimal OutputCostUsd { get; set; }

    /// <summary>Gets or sets the total cost in USD (input + output).</summary>
    public decimal TotalCostUsd => InputCostUsd + OutputCostUsd;

    /// <summary>Gets or sets the model that was used for this invocation.</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Gets or sets the pricing tier that was applied.</summary>
    public string PricingTier { get; set; } = string.Empty;

    /// <summary>Gets or sets the effective date of the pricing used.</summary>
    public DateTime PricingEffectiveDate { get; set; }

    /// <summary>Gets or sets when the cost was calculated.</summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Returns a human-readable cost breakdown.
    /// Example: "Input: $0.0001 (75 tokens) | Output: $0.0006 (200 tokens) | Total: $0.0007"
    /// </summary>
    public override string ToString() => $"Input: ${InputCostUsd:F6} ({InputTokens} tokens) | Output: ${OutputCostUsd:F6} ({OutputTokens} tokens) | Total: ${TotalCostUsd:F6}";
}

/// <summary>
/// Calculates the USD cost of Azure OpenAI token usage based on current pricing models.
/// Handles tiered pricing where rates change based on cumulative token volume.
/// </summary>
public class TokenCostCalculator
{
    private readonly ModelPricing _modelPricing;

    /// <summary>
    /// Initializes a new instance of TokenCostCalculator with explicit pricing.
    /// </summary>
    public TokenCostCalculator(ModelPricing modelPricing) => _modelPricing = modelPricing ?? throw new ArgumentNullException(nameof(modelPricing));

    /// <summary>
    /// Initializes a new instance of TokenCostCalculator for a named model.
    /// Looks up pricing from the catalog.
    /// </summary>
    public TokenCostCalculator(string modelName)
    {
        var pricing = PricingCatalog.GetPricingForModel(modelName);
        _modelPricing = pricing ?? throw new ArgumentException($"No pricing found for model: {modelName}", nameof(modelName));
    }

    /// <summary>
    /// Calculates the total cost for an AI invocation given input and output tokens.
    /// </summary>
    /// <param name="inputTokens">Number of input tokens (prompt).</param>
    /// <param name="outputTokens">Number of output tokens (completion).</param>
    /// <param name="cumulativeInputTokens">Optional: total input tokens to date (for tiered pricing). Defaults to current inputTokens.</param>
    /// <param name="cumulativeOutputTokens">Optional: total output tokens to date (for tiered pricing). Defaults to current outputTokens.</param>
    /// <returns>TokenCostResult with USD cost breakdown.</returns>
    public TokenCostResult CalculateCost(
        long inputTokens,
        long outputTokens,
        long cumulativeInputTokens = 0,
        long cumulativeOutputTokens = 0)
    {
        // Use current tokens if cumulative not provided (single invocation)
        if (cumulativeInputTokens == 0)
            cumulativeInputTokens = inputTokens;
        if (cumulativeOutputTokens == 0)
            cumulativeOutputTokens = outputTokens;

        // Find applicable tiers based on cumulative token counts
        var inputTier = FindApplicableTier(_modelPricing.Tiers, cumulativeInputTokens);
        var outputTier = FindApplicableTier(_modelPricing.Tiers, cumulativeOutputTokens);

        if (inputTier == null || outputTier == null)
        {
            throw new InvalidOperationException($"Could not find applicable pricing tier for model: {_modelPricing.ModelName}");
        }

        // Calculate costs: tokens / 1000 * price per 1K
        decimal inputCostUsd = (inputTokens / 1000m) * inputTier.InputTokenPricePer1k;
        decimal outputCostUsd = (outputTokens / 1000m) * outputTier.OutputTokenPricePer1k;

        return new TokenCostResult
        {
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            InputCostUsd = inputCostUsd,
            OutputCostUsd = outputCostUsd,
            ModelName = _modelPricing.ModelName,
            PricingTier = $"{inputTier.MinTokens}-{inputTier.MaxTokens}",
            PricingEffectiveDate = _modelPricing.EffectiveDate
        };
    }

    /// <summary>
    /// Calculates cost for multiple invocations, applying tiered pricing correctly.
    /// Useful for calculating batch costs or daily/monthly aggregates.
    /// </summary>
    /// <param name="invocations">List of (inputTokens, outputTokens) pairs.</param>
    /// <returns>List of costs per invocation and a summary total.</returns>
    public (List<TokenCostResult> results, TokenCostResult summary) CalculateBatchCost(
        params (int inputTokens, int outputTokens)[] invocations)
    {
        var results = new List<TokenCostResult>();
        long cumulativeInput = 0;
        long cumulativeOutput = 0;
        decimal totalInputCost = 0;
        decimal totalOutputCost = 0;

        foreach (var (input, output) in invocations)
        {
            var result = CalculateCost(input, output, cumulativeInput + input, cumulativeOutput + output);
            results.Add(result);

            cumulativeInput += input;
            cumulativeOutput += output;
            totalInputCost += result.InputCostUsd;
            totalOutputCost += result.OutputCostUsd;
        }

        var summary = new TokenCostResult
        {
            InputTokens = (int)cumulativeInput,
            OutputTokens = (int)cumulativeOutput,
            InputCostUsd = totalInputCost,
            OutputCostUsd = totalOutputCost,
            ModelName = _modelPricing.ModelName,
            PricingTier = "batch"
        };

        return (results, summary);
    }

    /// <summary>
    /// Calculates estimated monthly cost based on daily average.
    /// Useful for budgeting and cost projections.
    /// </summary>
    public (decimal dailyCostUsd, decimal monthlyCostUsd, decimal annualCostUsd) ProjectAnnualCost(
        int averageDailyInputTokens,
        int averageDailyOutputTokens)
    {
        var dailyCost = CalculateCost(averageDailyInputTokens, averageDailyOutputTokens).TotalCostUsd;
        var monthlyCost = dailyCost * 30;
        var annualCost = dailyCost * 365;

        return (dailyCost, monthlyCost, annualCost);
    }

    /// <summary>
    /// Finds the applicable pricing tier for a given cumulative token count.
    /// </summary>
    private static ModelPricingTier? FindApplicableTier(List<ModelPricingTier> tiers, long cumulativeTokens) =>
        // Tiers should be sorted by MinTokens. Find the tier where:
        // MinTokens <= cumulativeTokens < MaxTokens
        tiers.FirstOrDefault(t =>
            cumulativeTokens >= t.MinTokens && cumulativeTokens < t.MaxTokens);

    /// <summary>
    /// Gets a human-readable cost breakdown for logging/reporting.
    /// </summary>
    public string GetCostBreakdown(int inputTokens, int outputTokens)
    {
        var result = CalculateCost(inputTokens, outputTokens);
        return result.ToString();
    }
}
