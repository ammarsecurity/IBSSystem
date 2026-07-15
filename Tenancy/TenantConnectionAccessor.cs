namespace IBSMobile.Tenancy;

public interface ITenantConnectionAccessor
{
    string? CompanyKey { get; }
    string ConnectionString { get; }
    bool IsResolved { get; }

    void SetCompany(string companyKey, string connectionString);
    void Clear();
}

public class TenantConnectionAccessor : ITenantConnectionAccessor
{
    private readonly IConfiguration _configuration;
    private string? _companyKey;
    private string? _connectionString;

    public TenantConnectionAccessor(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? CompanyKey => _companyKey;

    public bool IsResolved => !string.IsNullOrWhiteSpace(_connectionString);

    public string ConnectionString =>
        !string.IsNullOrWhiteSpace(_connectionString)
            ? _connectionString!
            : _configuration.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException("Connection string is not configured.");

    public void SetCompany(string companyKey, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(companyKey))
            throw new ArgumentException("Company key is required.", nameof(companyKey));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        _companyKey = companyKey.Trim().ToUpperInvariant();
        _connectionString = connectionString.Trim();
    }

    public void Clear()
    {
        _companyKey = null;
        _connectionString = null;
    }
}
