namespace FinWise.MauiApp.Services;

public static class ChatHelperMethods
{
    public static readonly string[] SampleQuestions =
   [
       "How is my financial health this month?",
        "What are my biggest spending categories?",
        "Can you help me create a budget?",
        "How much should I save each month?",
        "How can I reduce overspending?",
        "Show me my recent transactions",
        "Can I afford a home worth 450,000 naira with my current income and debt?",
        "Can I afford a home worth 450,000 euro with my current income and debt?",
        "What monthly mortgage payment fits my budget?",
        "Should I choose a 15-year or 30-year mortgage based on my cash flow?",
        "How would refinancing my mortgage change my monthly payment and total interest?"
   ];

    public static TokenUsageInfo? ConvertToTokenUsageInfo(EnhancedTokenUsageTelemetry? telemetry) => telemetry?.Total is null
            ? null
            : new TokenUsageInfo
            {
                MeasuredInputTokens = (int)telemetry.Total.MeasuredInputTokens,
                MeasuredOutputTokens = (int)telemetry.Total.MeasuredOutputTokens,
                MeasuredTotalTokens = (int)telemetry.Total.MeasuredTotalTokens,
                EstimatedCostUsd = telemetry.Total.CostUsd,
                TokenMeasurementMode = telemetry.Mode.ToString(),
                ExactUsageAvailable = telemetry.ExactUsageAvailable
            };
}
