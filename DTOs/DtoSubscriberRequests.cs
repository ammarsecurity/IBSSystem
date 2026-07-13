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

    public string? ReturnUrl { get; set; }
}
