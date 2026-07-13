using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class MainAffiliate
    {
        [Key]
        public int Id { get; set; }
        public decimal AffIndex { get; set; }
        public string AffiliateName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Uri { get; set; }
        public string AffiliateType { get; set; }
        public string DepositPassword { get; set; }
        public decimal InitialCredit { get; set; }
        public decimal CriticalCredit { get; set; }
        public bool CanAddUser { get; set; }
        public bool CanEditUser { get; set; }


        public ICollection<Activation_User> Activation_Users { get; set; } = default!;
        public ICollection<SubAffiliate> SubAffiliates { get; set; } = default!;
        public ICollection<Subscriber> Subscribers { get; set; } = default!;
    }
}
