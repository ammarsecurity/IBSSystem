namespace IBSMobile.DTOs
{
    public class DtoActivation
    {
        public int id { get; set; }
        public DateTime creationDate { get; set; }
        public string profile { get; set; }
        public bool saleType { get; set; }
        public decimal cost { get; set; }
    }

    public class DtoReceivable
    {
        public int id { get; set; }
        public DateTime creationDate { get; set; }
        public decimal amount { get; set; }
    }
}
