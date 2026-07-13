namespace IBSMobile.DTOs
{
    public class DtoFtthTokenServerResponse
    {
        public string accessToken { get; set; }
        public DateTime accessTokenExp { get; set; }
        public string refreshToken { get; set; }
        public DateTime refreshTokenExp { get; set; }
    }

}
