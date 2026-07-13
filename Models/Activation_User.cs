using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBSMobile.Models
{
    public class Activation_User
    {
        [Key]
        public int Id { get; set; }
        public DateTime activation_date { get; set; }

        [ForeignKey("Employee_Id")]
        public User FK_Employee_Id { get; set; }
        public string Activation_Note { get; set; }

        [ForeignKey("Activation_SubscId")]
        public Subscriber FK_Activation_Activation_SubscId { get; set; }
        public string Activation_SubscUsername { get; set; }

        [ForeignKey("Activation_Profile")]
        public Profile FK_Activation_Profile { get; set; }

        [ForeignKey("Activation_MainAffiliateId")]
        public MainAffiliate FK_Activation_MainAffiliateId { get; set; }
      
        [ForeignKey("Activation_SubAffiliateId")]
        public SubAffiliate FK_Activation_SubAffiliateId { get; set; }
        public bool Activation_SaleType { get; set; }
        public decimal Activation_Cost { get; set; }
        public decimal Activation_Purchase { get; set; }

        [ForeignKey("Activation_Cash_Account")] 
        public Chart_Account FK_Activation_Cash_Account { get; set; }
        public bool Activation_IsActivated { get; set; }
        public string Activation_Source_Type { get; set; }

    }
}
