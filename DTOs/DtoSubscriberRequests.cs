using System.ComponentModel.DataAnnotations;

namespace IBSMobile.DTOs;

public class DtoRefillRequest
{
    public bool SaleType { get; set; }

    [Required]
    public int ProfileId { get; set; }
}

public class DtoCreatePaymentRequest
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>Required for Purpose=Refill. Ignored for debt payment.</summary>
    public int? ProfileId { get; set; }

    public bool SaleType { get; set; } = true;

    /// <summary>Refill = تجديد اشتراك, Debt = تسديد ديون سابقة. If omitted, inferred from ProfileId.</summary>
    public string? Purpose { get; set; }

    public string? ReturnUrl { get; set; }
}

public class DtoConfirmPaymentRequest
{
    [Required]
    public string PaymentId { get; set; } = string.Empty;

    public string? RequestId { get; set; }

    public string? Status { get; set; }
}
