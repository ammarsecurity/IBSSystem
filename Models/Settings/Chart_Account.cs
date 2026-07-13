using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class Chart_Account
    {
        [Key]
        public int Ch_Id { get; set; }
        public string Ch_Description { get; set; }
        public int Ch_Emp_Id { get; set; }
        public int Chart_Type { get; set; }
        public decimal Initial_Credit { get; set; }


        public ICollection<Activation_User> Activation_Users { get; set; } = default!;
        public ICollection<Receivable> Receivables { get; set; } = default!;
        public ICollection<Subscriber> Subscribers { get; set; } = default!;


        public ICollection<User> users { get; set; } = default!;
        public ICollection<User> users2 { get; set; } = default!;
        public ICollection<User> users3 { get; set; } = default!;

    }
}
