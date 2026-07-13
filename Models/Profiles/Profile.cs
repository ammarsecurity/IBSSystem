using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class Profile
    {
        [Key]
        public int Id { get; set; }
        public int AccIndex { get; set; }
        public string Account_Name { get; set; }
        public string Account_Description { get; set; }
        public int Account_MainAffiliate { get; set; }
        public decimal Account_BuyCost { get; set; }
        public decimal Account_Price { get; set; }
        public bool Account_Active { get; set; }


        public ICollection<Activation_User> Activation_Users { get; set; } = default!;
    }
}
