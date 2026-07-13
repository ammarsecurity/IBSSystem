using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class SubscribersCredit
    {
        public int SubscId { get; set; }
        public decimal current_credit { get; set; }
        public DateTime? active_date { get; set; }
        public int chart_account { get; set; }
    }
}
