using System.Security.Claims;

namespace IBSMobile.Tenancy;

/// <summary>
/// Resolves the tenant database from the JWT Company claim after authentication.
/// Login sets the tenant explicitly before querying.
/// </summary>
public class TenantResolutionMiddleware
{
    public const string CompanyClaimType = "Company";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantConnectionAccessor tenant,
        ICompanyConnectionResolver resolver)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var companyClaim = context.User.FindFirstValue(CompanyClaimType)
                ?? context.User.FindFirstValue("company");

            if (!string.IsNullOrWhiteSpace(companyClaim) &&
                resolver.TryResolve(companyClaim, out var key, out var connectionString, out _))
            {
                tenant.SetCompany(key, connectionString);
            }
        }

        await _next(context);
    }
}
