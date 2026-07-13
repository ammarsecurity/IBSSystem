using System.ComponentModel.DataAnnotations;

namespace IBS.Data
{
    public class DtoCreatePaymentIntent
    {        
        public decimal? Amount { get; set; }

        // Optional hint to control post-payment behavior (e.g. custom plan extension)
        public string? Purpose { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class DtoCreateSubscription
    {
        [Required]
        public int PricingPlanId { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class DtoConfirmPayment
    {
        [Required]
        public string PaymentIntentId { get; set; } = string.Empty;

        [Required]
        public string PaymentMethodId { get; set; } = string.Empty;
    }

    public class DtoConfirmSubscription
    {
        [Required]
        public string SubscriptionId { get; set; } = string.Empty;

        [Required]
        public string PaymentMethodId { get; set; } = string.Empty;
    }


    public class DtoCreatePaymentObject
    {
        public string token { get; set; } //ccd589cc-fd7c-4597-86d4-8a8b62b0573b
        public string AppId { get; set; }
        public string requestId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public DtoPaymentCustomerObj? customerInfo { get; set; }
        public string returnUrl { get; set; }

    }

    public class DtoPaymentCustomerObj
    {
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string address { get; set; }
    }

    public class DtoDtoPaymentReturnResponse
    {
        public bool error { get; set; }
        public string? message { get; set; }
        public DtoPaymentReturn? data { get; set; }
    }
    public class DtoPaymentReturn
    {
        public int paymentId { get; set; }
        public string formUrl { get; set; }
        public string qiPaymentId { get; set; }
        public string requestId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
    }

    //public class DtoPaymentReturnObj
    //{
    //    public string requestId { get; set; }
    //    public string paymentId { get; set; }
    //    public string status { get; set; }
    //    public bool canceled { get; set; }
    //    public decimal amount { get; set; }
    //    public string currency { get; set; }
    //    public DateTime creationDate { get; set; }
    //    public string formUrl { get; set; }
    //    public bool withoutAuthenticate { get; set; }
    //    public bool appChannel { get; set; }
    //}


    public class DtoPaymentResponseStatus
    {
        public string requestId { get; set; }
        public string paymentId { get; set; }
        public string status { get; set; }
        public bool canceled { get; set; }
        public decimal amount { get; set; }
        public decimal confirmedAmount { get; set; }
        public string currency { get; set; }
        public string paymentType { get; set; }
        public DateTime creationDate { get; set; }
        public DtoPymentResponseDetails details { get; set; }
        public bool withoutAuthenticate { get; set; }
        public bool appChannel { get; set; }
        public bool? autoCanceled { get; set; }
        public DtoAdditianalInfo? additionalInfo { get; set; }
    }


    public class DtoAdditianalInfo
    {
        public string? key1 { get; set; }
        public string? key2 { get; set; }
    }

    public class DtoPymentResponseDetails
    {
        public string resultCode { get; set; }
        public string rrn { get; set; }
        public string authId { get; set; }
        public DateTime authDate { get; set; }
        public string maskedPan { get; set; }
        public string paymentSystem { get; set; }
        public object? customDetails { get; set; }
    }


   public class DtoPaymentFinish
    {
        public string requestId { get; set; }
        public string paymentId { get; set; }
        public string paymentType { get; set; }
        public string status { get; set; }

    }


    public class DtoStatusResponse
    {
        public bool error { get; set; }
        public string? message{ get; set; }
        public DtoPaymentResponseStatus? data { get; set; }
    }

    public class DtoPaymentErrorResponse
    {
        public DtoPaymentError error { get; set; }
    }
    public class DtoPaymentError
    {
        public int code { get; set; }
        public string description { get; set; }
    }


    public class DtoPaymentStatusReturn
    {
        public int Id { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? FailureReason { get; set; }
    }

}