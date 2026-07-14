namespace FinWise.BudgetingAgent.ToshlApi.Contracts;

public interface IPlanningModule
{
    Task<PlanningOverview> ForecastAsync(string? from = null, string? to = null);
}
