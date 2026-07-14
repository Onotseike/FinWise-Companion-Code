using FinWise.Shared.Core.AgentFramework;

namespace FinWise.BudgetingAgent.Models;

/// <summary>
/// Context model for Budgeting Agent operations.
/// Extends BaseAgentContext with budget-specific data for budget analysis, tracking, and advice generation.
/// </summary>
public class BudgetingAgentContext : BaseAgentContext
{
    /// <summary>Gets or sets the user's current account summary.</summary>
    public AccountSummary? AccountSummary { get; set; }

    /// <summary>Gets or sets recent transactions for analysis.</summary>
    public List<Transaction> RecentTransactions { get; set; } = [];

    /// <summary>Gets or sets the user's budget configurations.</summary>
    public List<BudgetConfiguration> Budgets { get; set; } = [];

    /// <summary>Gets or sets the user's expense categories.</summary>
    public List<ExpenseCategory> ExpenseCategories { get; set; } = [];
}

/// <summary>
/// Represents a user's account summary (balance, income, etc.)
/// </summary>
public class AccountSummary
{
    public string UserId { get; set; } = string.Empty;
    public decimal AccountBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public DateTime SummaryDate { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

/// <summary>
/// Represents a financial transaction.
/// </summary>
public class Transaction
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

/// <summary>
/// Transaction types.
/// </summary>
public enum TransactionType
{
    Income,
    Expense,
    Transfer
}

/// <summary>
/// Represents a user's budget configuration.
/// </summary>
public class BudgetConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal PlannedAmount { get; set; }
    public decimal CurrentSpent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<BudgetRule> Rules { get; set; } = [];
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

/// <summary>
/// A rule associated with a budget.
/// </summary>
public class BudgetRule
{
    public string RuleType { get; set; } = string.Empty; // e.g., "alert_threshold", "auto_transfer"
    public Dictionary<string, object?> RuleParameters { get; set; } = [];
}

/// <summary>
/// Represents an expense category used by the user.
/// </summary>
public class ExpenseCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal AverageMonthlySpending { get; set; }
    public int TransactionCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
