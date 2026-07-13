namespace IBSMobile.DTOs
{
    public class Customer
    {
        public int id { get; set; }
        public string customerFullName { get; set; }
        public string customerPhoneNumber { get; set; }
        public string customerSecondPhoneNumber { get; set; }
        public string email { get; set; }
        public string address { get; set; }
        public DateTime? createdAt { get; set; }
        public DateTime? updatedAt { get; set; }
        public bool canAttachSubscription { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
    }
}
