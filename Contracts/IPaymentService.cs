using IBSMobile.Data;
using IBSMobile.Models;
using IBSMobile.Statics;

namespace IBS.Interfaces
{
    public interface IPaymentService
    {
        Task<decimal> CalculatePaymentAmountAsync(int pricingPlanId);
        Task<Response> CreatePaymentIntentAsync(int pricingPlanId, decimal? amount = null, string? purpose = null, string? returnUrl = null);
        Task<Response> CreateSubscriptionAsync(int pricingPlanId, string? returnUrl = null);
        Task<Response> GetPaymentStatusFromProviderAsync(string paymentId);
        Task<Response> GetQiPaymentStatusFromProviderAsync(string paymentId);
        Task<ResponseNoData> ConfirmPaymentAsync(string paymentIntentId, string paymentMethodId);
        Task<ResponseNoData> ConfirmSubscriptionAsync(string subscriptionId, string paymentMethodId);
        Task<Response> GetPaymentStatusAsync(string paymentId);
        Task<Response> GetPaymentStatusByRequestIdAsync(string requestId);
    }
}
