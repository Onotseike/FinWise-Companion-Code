using FinWise.Shared.Core.AgentFramework;

namespace FinWise.LoanAgent.Models;

/// <summary>
/// Context model for Loan Agent operations.
/// Extends BaseAgentContext with loan-specific data for mortgage analysis, loan management, and property evaluation.
/// </summary>
public class LoanAgentContext : BaseAgentContext
{
    /// <summary>Gets or sets the user's financial context from budgeting agent.</summary>
    public FinancialContext? FinancialContext { get; set; }

    /// <summary>Gets or sets properties being analyzed.</summary>
    public List<Property> Properties { get; set; } = [];

    /// <summary>Gets or sets loan scenarios being evaluated.</summary>
    public List<LoanScenario> LoanScenarios { get; set; } = [];

    /// <summary>Gets or sets mortgage recommendations.</summary>
    public List<MortgageRecommendation> Recommendations { get; set; } = [];
}

/// <summary>
/// Represents financial context from the budgeting agent.
/// </summary>
public class FinancialContext
{
    public string UserId { get; set; } = string.Empty;
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal AvailableSavings { get; set; }
    public decimal DebtCommitments { get; set; }
    public int CreditScore { get; set; }
    public Dictionary<string, object?> BudgetAdvice { get; set; } = [];
    public Dictionary<string, object?> SpendingAnalysis { get; set; } = [];
}

/// <summary>
/// Represents a property available for mortgage.
/// </summary>
public class Property
{
    public string Id { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public decimal SquareFeet { get; set; }
    public string PropertyType { get; set; } = string.Empty; // "house", "condo", "apartment"
    public DateTime ListDate { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

/// <summary>
/// Represents a loan scenario being evaluated.
/// </summary>
public class LoanScenario
{
    public string Id { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public decimal DownPayment { get; set; }
    public int LoanTerm { get; set; } // in months
    public decimal InterestRate { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal TotalInterest { get; set; }
    public string LoanType { get; set; } = string.Empty; // "fixed", "variable"
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

/// <summary>
/// Represents a mortgage recommendation.
/// </summary>
public class MortgageRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public decimal RecommendedDownPayment { get; set; }
    public int RecommendedLoanTerm { get; set; }
    public string RecommendationType { get; set; } = string.Empty; // "standard", "aggressive", "conservative"
    public string Rationale { get; set; } = string.Empty;
    public List<string> RiskFactors { get; set; } = [];
    public List<string> Considerations { get; set; } = [];
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
