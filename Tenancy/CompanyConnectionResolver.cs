using Microsoft.Extensions.Options;

namespace IBSMobile.Tenancy;

public interface ICompanyConnectionResolver
{
    bool TryResolve(string? companyInput, out string companyKey, out string connectionString, out string? error);
    IReadOnlyCollection<string> GetKnownCompanyKeys();
}

public class CompanyConnectionResolver : ICompanyConnectionResolver
{
    private readonly TenantDatabaseOptions _options;

    public CompanyConnectionResolver(IOptions<TenantDatabaseOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyCollection<string> GetKnownCompanyKeys() =>
        _options.Companies.Keys.OrderBy(k => k).ToArray();

    public bool TryResolve(string? companyInput, out string companyKey, out string connectionString, out string? error)
    {
        companyKey = string.Empty;
        connectionString = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(companyInput))
        {
            error = "يجب إدخال اسم الشركة التابعة.";
            return false;
        }

        var input = Normalize(companyInput);

        foreach (var (key, company) in _options.Companies)
        {
            if (string.IsNullOrWhiteSpace(company.ConnectionString))
                continue;

            if (Normalize(key) == input)
            {
                companyKey = key.ToUpperInvariant();
                connectionString = company.ConnectionString.Trim();
                return true;
            }

            foreach (var alias in company.Aliases)
            {
                if (Normalize(alias) == input)
                {
                    companyKey = key.ToUpperInvariant();
                    connectionString = company.ConnectionString.Trim();
                    return true;
                }
            }
        }

        error = "الشركة التابعة غير معروفة. تأكد من الاسم أو أضفه في إعدادات TenantDatabases.";
        return false;
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
    }
}
