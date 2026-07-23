using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBSMobile.Models
{
    public class Receivable
    {
        [Key]
        public int RecId { get; set; }
        public DateTime Rec_Date { get; set; }
        public decimal Rec_Amount { get; set; }

        public int Rec_SubscId { get; set; }

        [ForeignKey(nameof(Rec_SubscId))]
        public Subscriber? FK_Receivables_Rec_SubscId { get; set; }

        public string Rec_Note { get; set; } = null!;

        public int Rec_empid { get; set; }

        [ForeignKey(nameof(Rec_empid))]
        public User? FK_Receivables_Rec_empid { get; set; }

        public int Rec_Cash_Account { get; set; }

        [ForeignKey(nameof(Rec_Cash_Account))]
        public Chart_Account? FK_Receivables_Rec_Cash_Account { get; set; }
    }
}
