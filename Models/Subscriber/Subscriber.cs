using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBSMobile.Models
{
    public class Subscriber
    {
        [Key]
        public int Id { get; set; }
        public decimal UserIndex { get; set; }
        public string NameStr { get; set; }
        public string Address { get; set; }
        public decimal Mobile { get; set; }
        public string Email { get; set; }
        public string Note { get; set; }
        public bool IsValid { get; set; }

        [ForeignKey("Employee")]
        public User FK_Subscribers_Employee { get; set; }

        [ForeignKey("AccountId")]
        public Chart_Account AccountCh_Id { get; set; }


        [ForeignKey("MainAffiliate")]
        public MainAffiliate FK_Subscribers_MainAffiliate { get; set; }

        [ForeignKey("SubAffiliate")]
        public SubAffiliate FK_Subscribers_SubAffiliate { get; set; }
        public string Username { get; set; }
        public decimal InitialCredit { get; set; }
        public int PaymentMethod { get; set; }
        public int LateDays { get; set; }
        public int OweStatus { get; set; }
        public decimal MAX_Credit { get; set; }
        public int Subsc_DeviceId { get; set; }


        public ICollection<Activation_User> Activation_Users { get; set; } = default!;
        public ICollection<Receivable> Receivables { get; set; } = default!;

    }
}
