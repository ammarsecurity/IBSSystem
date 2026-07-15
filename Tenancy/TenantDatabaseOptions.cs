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
}
