using System.Security.Claims;

namespace IBSMobile.Tenancy;

/// <summary>
/// Resolves the tenant database from the JWT Company claim after authentication,
/// or from X-Company / company query for anonymous payment return callbacks.
/// </summary>
public class TenantResolutionMiddleware
{
    public const string CompanyClaimType = "Company";
    public const string CompanyHeaderName = "X-Company";

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
        string? companyHint = null;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            companyHint = context.User.FindFirstValue(CompanyClaimType)
                ?? context.User.FindFirstValue("company");
        }

        if (string.IsNullOrWhiteSpace(companyHint))
        {
            companyHint = context.Request.Headers[CompanyHeaderName].FirstOrDefault()
                ?? context.Request.Query["company"].FirstOrDefault();
        }

        // Qi sometimes appends with "?" and corrupts values: "KGD?requestId=..."
        if (!string.IsNullOrWhiteSpace(companyHint))
            companyHint = companyHint.Split('?', 2)[0].Split('&', 2)[0].Trim();

        if (!string.IsNullOrWhiteSpace(companyHint) &&
            resolver.TryResolve(companyHint, out var key, out var connectionString, out _))
        {
            tenant.SetCompany(key, connectionString);
        }

        await _next(context);
    }
}
