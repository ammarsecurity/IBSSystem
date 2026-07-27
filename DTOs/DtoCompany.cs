namespace IBSMobile.DTOs;

public class DtoCompany
{
    /// <summary>Value sent as login company (alias preferred).</summary>
    public string Id { get; set; } = "";

    public string Label { get; set; } = "";

    public string Hint { get; set; } = "";

    public string LogoUrl { get; set; } = "";
}
