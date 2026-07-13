using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class ProfileCostCustomizer
    {
        [Key]
        public int Id { get; set; }
        public string Description { get; set; } = null!;
    }
}