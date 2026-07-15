namespace IBSMobile.DTOs
{
    public class LoginRequestDto
    {
        public string mobile { get; set; } = string.Empty;
        public string company { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public int userId { get; set; }
        public string? fullName { get; set; }
        public string? mobile { get; set; }
        public string? accessToken { get; set; }
        public string? company { get; set; }
        public bool isSuccess { get; set; }
        public string? error { get; set; }
        public DateTime? issuedAt { get; set; }
        public DateTime? expiresAt { get; set; }
    }

}
