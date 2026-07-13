using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class SubscriberApp
    {
        [Key]
        public int Id { get; set; }
        public int SubscId { get; set; }
        public bool CanActiveNoCash { get; set; }
        public decimal AmountDue { get; set; }
    }
}
