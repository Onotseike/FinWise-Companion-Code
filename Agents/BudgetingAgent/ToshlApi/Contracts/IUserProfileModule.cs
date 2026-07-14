namespace FinWise.BudgetingAgent.ToshlApi.Contracts;

public interface IUserProfileModule
{
    Task<User> GetProfileAsync();
    Task<Summary> GetAccountSummaryAsync(string? from = null, string? to = null);
    Task<PaymentType[]> GetPaymentTypesAsync();
    Task<Payment[]> GetPaymentsAsync();
}
