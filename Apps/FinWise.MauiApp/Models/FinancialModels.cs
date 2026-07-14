namespace FinWise.MauiApp.Models;

public record ChatRequest(string Message);

public record ChatResponse(string Response);

public record AdviceResponse(string Advice);

public record AnalysisResponse(string Analysis);

public record BudgetResponse(string Budget);

public class FinancialInsight
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public FinancialInsightType Type { get; set; }
}

public enum FinancialInsightType
{
    BudgetAdvice,
    SpendingAnalysis,
    PersonalizedBudget,
    GeneralInsight
}

public class FinancialSummary
{
    public string UserProfile { get; set; } = string.Empty;
    public string AccountSummary { get; set; } = string.Empty;
    public string RecentTransactions { get; set; } = string.Empty;
    public string Budgets { get; set; } = string.Empty;
    public string Categories { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}   