using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class ApiServerSetting
    {
        [Key]
        public int Id { get; set; }
        public string SelectedServer { get; set; } = "Saturn";
    }
}
