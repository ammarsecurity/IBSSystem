using IBSMobile.Contracts;
using IBSMobile.Data;
using IBSMobile.DTOs;
using IBSMobile.Functions;
using IBSMobile.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IBSMobile.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IBSFunctions _function;

    public AuthService(IConfiguration configuration, ApplicationDbContext db, IBSFunctions function)
    {
        _db = db;
        _configuration = configuration;
        _function = function;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        var destination = _function.FormatNumber(dto.mobile);
        
        var user = await _db.Subscribers.FirstOrDefaultAsync(
            u => u.Mobile == destination && u.IsValid, cancellationToken);
        if (user == null)
            throw new ArgumentException("اسم المستخدم غير صحيح.");

        var userApp = await _db.UserApp.FirstOrDefaultAsync();
        if (userApp == null)
            throw new ArgumentException("اعدادت التطبيق غير صحيحة.");

        var expires = DateTime.Now.AddMonths(1);

        var token = GenerateJwtToken(user);
        _db.SubscriberAccessTokens.Add(new SubscriberAccessToken
        {
            SubscriberId = user.Id,
            accessToken = token,
            ExpirationDate = DateTime.Now.AddMonths(1)
        });
        await _db.SaveChangesAsync(cancellationToken);

        var strMobile = _function.FormatNumberString(destination);

        return new AuthResponseDto
        {
            userId = user.Id,
            fullName = user.NameStr,
            mobile = strMobile,
            accessToken = token,
            expiresAt = expires,
            isSuccess = true
        };
    }

    private string GenerateJwtToken(Subscriber user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "cRun2thOqorIci6ibufis6cRa1raTrur"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
            };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "IBS",
            audience: _configuration["Jwt:Audience"] ?? "IBS",
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


}
