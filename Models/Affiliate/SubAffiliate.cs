using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class SubAffiliate
    {
        [Key]
        public int Id { get; set; }
        public int AffIndex { get; set; }
        public string AffName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int MainAffiliate { get; set; }
        public decimal MaxCredit { get; set; }
        public bool State { get; set; }
    }
}
