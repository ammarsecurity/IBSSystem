namespace IBSMobile.DTOs
{
    public class DtoFinancialInfo
    {
        public int userId { get; set; }
        public decimal AmountDue { get; set; }
        public decimal? DebtLimit { get; set; }
        public bool CanActiveNoCash { get; set; }
        public DateTime? LastActivation { get; set; }
    }
}
