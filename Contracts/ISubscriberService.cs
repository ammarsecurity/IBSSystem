using IBSMobile.DTOs;
using IBSMobile.Statics;

namespace IBSMobile.Contracts;

public interface ISubscriberService
{
    Task<DtoFinancialInfo> FinancialInfoAsync(int userId);
    Task<Response> SubscriptionInfoAsync(int userId);
    Task<List<DtoProfilePackage>> GetPackagesAsync(int userId);
    Task<Response> RefillSubscriptionAsync(int userId, bool saleType, int profileId);
    Task<decimal> GetAmountDue(int userId);
    Task<List<DtoActivation>> GetInvoicesAsync(int userId);
    Task<List<DtoReceivable>> GetReceivableAsync(int userId);
    Task<Response> PayBackAmountAsync(int userId);
    Task<Response> CreatePaymentAsync(int subscriberId, decimal amount, string? returnUrl = null);
}
