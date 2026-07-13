using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBSMobile.Models
{
    public enum PaymentStatus
    {
        Pending = 0,
        Processing = 1,
        Succeeded = 2,
        Failed = 3,
        Canceled = 4,
        Refunded = 5
    }

    public enum PaymentType
    {
        OneTime = 1,
        Subscription = 2
    }

    public class Payment 
    {
        [Key]
        public int Id { get; set; }

        public int SubscriberId { get; set; }

        [MaxLength(256)]
        public string? PaymentId { get; set; }


        [MaxLength(256)]
        public string? RequestId { get; set; }


        [Column(TypeName = "decimal(18, 4)")]
        [Required]
        public decimal Amount { get; set; }

        [MaxLength(10)]
        [Required]
        public string Currency { get; set; } = "IQD";

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [Required]
        public PaymentType Type { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(1000)]
        public string? FailureReason { get; set; }

        [MaxLength(100)]
        public string? Purpose { get; set; }

        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }

        public bool IsReceivedFromUfeq { get; set; } = false;
        public DateTime? ReceivingDate { get; set; }
        public decimal Fee { get; set; }

    }
}
