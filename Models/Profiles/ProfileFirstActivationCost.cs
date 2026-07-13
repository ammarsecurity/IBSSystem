using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class ProfileFirstActivationCost
    {
        [Key]
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public decimal PurchaseCost { get; set; }
        public decimal SaleCost { get; set; }
    }
}
