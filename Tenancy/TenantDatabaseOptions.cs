namespace IBSMobile.Tenancy;

public class TenantDatabaseOptions
{
    public const string SectionName = "TenantDatabases";

    /// <summary>Fallback company key when none is resolved.</summary>
    public string Default { get; set; } = "WAEL";

    public Dictionary<string, TenantCompanyOptions> Companies { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class TenantCompanyOptions
{
    /// <summary>Optional display aliases users may type at login (e.g. Wi-Fi, wifi).</summary>
    public List<string> Aliases { get; set; } = [];

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Name shown on the login company tile.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Short Arabic (or secondary) hint under the display name.</summary>
    public string Hint { get; set; } = "";

    /// <summary>Public logo path, e.g. /logos/wael.png</summary>
    public string LogoUrl { get; set; } = "";

    public int SortOrder { get; set; }
}
