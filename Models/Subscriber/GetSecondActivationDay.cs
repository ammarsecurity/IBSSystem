using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class GetSecondActivationDay
    {
        [Key]
        public int Id { get; set; }
        public int ActDays { get; set; }
    }
}
