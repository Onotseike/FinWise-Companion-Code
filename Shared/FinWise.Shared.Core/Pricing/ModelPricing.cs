namespace FinWise.Shared.Core.Pricing;

/// <summary>
/// Represents pricing information for an Azure OpenAI model.
/// Pricing is tiered based on token volume thresholds.
/// All prices in USD per 1,000 tokens (1K tokens).
/// </summary>
public class ModelPricingTier
{
    /// <summary>Gets or sets the minimum number of tokens for this pricing tier (inclusive).</summary>
    public long MinTokens { get; set; }

    /// <summary>Gets or sets the maximum number of tokens for this pricing tier (exclusive). Long.MaxValue = no upper limit.</summary>
    public long MaxTokens { get; set; } = long.MaxValue;

    /// <summary>Gets or sets the price per 1,000 input tokens in this tier (USD).</summary>
    public decimal InputTokenPricePer1k { get; set; }

    /// <summary>Gets or sets the price per 1,000 output tokens in this tier (USD).</summary>
    public decimal OutputTokenPricePer1k { get; set; }
}

/// <summary>
/// Pricing information for an Azure OpenAI model.
/// Supports tiered pricing based on token volume.
/// </summary>
public class ModelPricing
{
    /// <summary>Gets or sets the model name (e.g., "gpt-5-nano", "gpt-4-turbo").</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Gets or sets the deployment name on Azure OpenAI.</summary>
    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is the active pricing model.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the effective date of this pricing.</summary>
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets pricing tiers (sorted by MinTokens ascending).</summary>
    /// <remarks>
    /// Example for gpt-5-nano (as of April 2026):
    /// - Tier 1 (0-10M tokens): $0.075 input, $0.3 output per 1K
    /// - Tier 2 (10M-50M tokens): $0.06 input, $0.24 output per 1K
    /// - Tier 3 (50M+ tokens): $0.045 input, $0.18 output per 1K
    /// </remarks>
    public List<ModelPricingTier> Tiers { get; set; } = [];
}

/// <summary>
/// Catalog of model pricing definitions.
/// Defines all supported models and their pricing structures.
/// </summary>
public static class PricingCatalog
{
    /// <summary>
    /// GPT-5 Nano pricing (as of April 2026).
    /// Ultra-efficient model for financial summarization and routing.
    /// </summary>
    public static readonly ModelPricing GptNanoPricing = new()
    {
        ModelName = "gpt-5-nano",
        DeploymentName = "gpt-5-nano-finwise",
        IsActive = true,
        EffectiveDate = new DateTime(2026, 4, 1),
        Tiers = new List<ModelPricingTier>
        {
            new()
            {
                MinTokens = 0,
                MaxTokens = 10_000_000,
                InputTokenPricePer1k = 0.075m,
                OutputTokenPricePer1k = 0.3m
            },
            new()
            {
                MinTokens = 10_000_000,
                MaxTokens = 50_000_000,
                InputTokenPricePer1k = 0.06m,
                OutputTokenPricePer1k = 0.24m
            },
            new()
            {
                MinTokens = 50_000_000,
                MaxTokens = long.MaxValue,
                InputTokenPricePer1k = 0.045m,
                OutputTokenPricePer1k = 0.18m
            }
        }
    };

    /// <summary>
    /// GPT-4 Turbo pricing (fallback if nano not available).
    /// Higher cost, higher capability.
    /// </summary>
    public static readonly ModelPricing Gpt4TurboPricing = new()
    {
        ModelName = "gpt-4-turbo",
        DeploymentName = "gpt-4-turbo-finwise",
        IsActive = false,
        EffectiveDate = new DateTime(2026, 4, 1),
        Tiers = new List<ModelPricingTier>
        {
            new()
            {
                MinTokens = 0,
                MaxTokens = long.MaxValue,
                InputTokenPricePer1k = 0.01m,
                OutputTokenPricePer1k = 0.03m
            }
        }
    };

    /// <summary>
    /// Get active pricing for a specific model.
    /// </summary>
    public static ModelPricing? GetPricingForModel(string modelName)
    {
        return modelName.ToLowerInvariant() switch
        {
            "gpt-5-nano" => GptNanoPricing,
            "gpt-5-nano-finwise" => GptNanoPricing,
            "gpt-4-turbo" => Gpt4TurboPricing,
            "gpt-4-turbo-finwise" => Gpt4TurboPricing,
            _ => GptNanoPricing // Default to nano
        };
    }
}
