using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class ProfileCost
    {
        [Key]
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public int SubAffiliateId { get; set; }
        public decimal SaleCost { get; set; }
    }
}
