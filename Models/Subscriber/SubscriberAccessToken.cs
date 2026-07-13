using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBSMobile.Services
{
    [Table("accessTokens")]
    public class SubscriberAccessToken
    {
        [Key]
        public int Id { get; set; }

        [Column("UserId")]
        public int SubscriberId { get; set; }

        public string accessToken { get; set; }

        [Column("expire")]
        public DateTime ExpirationDate { get; set; }
    }
}
