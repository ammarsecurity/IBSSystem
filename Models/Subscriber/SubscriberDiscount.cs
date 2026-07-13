using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBSMobile.Models
{
    [Table("SubscriberDiscount")]
    public class SubscriberDiscount
    {
        [Key]
        public int Id { get; set; }
        public int SubscId { get; set; }
        public decimal Amount { get; set; }
    }
}
