namespace IBSMobile.Models
{
    public class Get_Tansaction_History_Subscriber
    {
        public decimal balance { get; set; }
        public decimal credit { get; set; }
        public decimal debt { get; set; }
        public string details { get; set; }
        public int Nmbr { get; set; }
        public string datet { get; set; }
        public string transaction_type { get; set; }
        public DateTime tran_datetime { get; set; }
    }
}
