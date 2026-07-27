using IBSMobile.Contracts;
using IBSMobile.DTOs;
using IBSMobile.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IBSMobile.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly TenantDatabaseOptions _tenants;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        IAuthService authService,
        IOptions<TenantDatabaseOptions> tenants,
        IWebHostEnvironment env)
    {
        _authService = authService;
        _tenants = tenants.Value;
        _env = env;
    }

    [HttpGet("companies")]
    [AllowAnonymous]
    public ActionResult<List<DtoCompany>> GetCompanies()
    {
        var list = _tenants.Companies
            .Where(c => !string.IsNullOrWhiteSpace(c.Value.ConnectionString))
            .OrderBy(c => c.Value.SortOrder)
            .ThenBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
            .Select(c =>
            {
                var key = c.Key;
                var opts = c.Value;
                var loginId = opts.Aliases.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a))
                    ?? key;
                var label = string.IsNullOrWhiteSpace(opts.DisplayName) ? loginId : opts.DisplayName;
                var logoPath = ResolveLogoPath(key, opts.LogoUrl);

                return new DtoCompany
                {
                    Id = loginId,
                    Label = label,
                    Hint = opts.Hint ?? "",
                    LogoUrl = ToAbsoluteUrl(logoPath),
                };
            })
            .ToList();

        return Ok(list);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _authService.LoginAsync(dto, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private string ResolveLogoPath(string companyKey, string? configured)
    {
        var configuredPath = string.IsNullOrWhiteSpace(configured)
            ? $"/logos/{companyKey.Trim().ToLowerInvariant()}.png"
            : configured.Trim();

        if (!configuredPath.StartsWith('/'))
            configuredPath = "/" + configuredPath;

        var webRoot = _env.WebRootPath;
        if (!string.IsNullOrWhiteSpace(webRoot))
        {
            var physical = Path.Combine(
                webRoot,
                configuredPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical))
                return configuredPath;
        }

        return "/logos/default.png";
    }

    private string ToAbsoluteUrl(string path)
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}{path}";
    }
}
