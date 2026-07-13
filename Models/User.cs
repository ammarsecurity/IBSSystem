using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBSMobile.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string UserEmployeeName { get; set; }
        public string UserAddress { get; set; }
        public string UserJob { get; set; }
        public decimal UserMobile { get; set; }
        public string UserEmail { get; set; }
        public string UserACUsername { get; set; }
        public string UserACPassword { get; set; }
        public bool UserActive { get; set; }
        public bool UserIsAdmin { get; set; }

        [ForeignKey("UserCashAccount")]
        public Chart_Account FK_UserCashAccount { get; set; }


        [ForeignKey("UserSubscAccount")]
        public Chart_Account FK_UserSubscAccount { get; set; }

        public bool UserActiveView { get; set; }
        public bool UserActiveAdd { get; set; }
        public bool UserActiveChangeCost { get; set; }
        public bool UserActiveSelectCost { get; set; }
        public bool UserReceivables { get; set; }
        public bool UserRevenue { get; set; }
        public bool UserPayables { get; set; }
        public bool UserSubscAdd { get; set; }
        public bool UserSubscEdit { get; set; }
        public bool UserSubscDelete { get; set; }
        public bool UserSubscTest { get; set; }
        public bool UserRepSales { get; set; }
        public bool UserRepBalanceTrans { get; set; }
        public bool UserRepDebits { get; set; }
        public bool UserRepCashTrans { get; set; }
        public bool UserRepPayables { get; set; }
        public bool UserRepRevenues { get; set; }
        public bool UserRepReceivalbles { get; set; }
        public bool CanActiveInCritical { get; set; }
        public bool UserBuyBalance { get; set; }
        public bool UserDelete { get; set; }
        public bool UserEdit { get; set; }


        [ForeignKey("UserAgentsAccount")]
        public Chart_Account FK_UserAgentsAccount { get; set; }
        public bool UserSaleAgent { get; set; }
        public bool UserRecAgent { get; set; }
        public bool UserUserActions { get; set; }
        public decimal UserMaxCredit { get; set; }
        public decimal UserMaxDebt { get; set; }
        public bool UserSyncUsers { get; set; }
        public bool UserType { get; set; }
        public bool CanCashTransfer { get; set; }



        public ICollection<Activation_User> Activation_Users { get; set; } = default!;
        public ICollection<Receivable> Receivables { get; set; } = default!;
        public ICollection<Subscriber> Subscribers { get; set; } = default!;
    }
}
