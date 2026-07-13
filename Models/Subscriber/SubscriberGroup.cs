using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class SubscriberGroup
    {
        [Key]
        public int Id { get; set; }
        public int AppGroup { get; set; }
        public string ResellerGroup { get; set; } 
    }
}
