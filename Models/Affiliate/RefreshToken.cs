using System.ComponentModel.DataAnnotations;

namespace IBSMobile.Models
{
    public class RefreshTokens
    {
        [Key]
        public int Id { get; set; }
        public int Affiliate { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpire { get; set; }
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpire { get; set; }
    }
}
