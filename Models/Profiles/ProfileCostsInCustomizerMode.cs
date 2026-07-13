using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models.Profiles
{
    public class ProfileCostsInCustomizerMode
    {
        [Key]
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public int CostModeId { get; set; }
        public decimal SaleCost { get; set; }
    }
}
