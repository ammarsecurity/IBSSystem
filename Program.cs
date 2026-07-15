using System.Text;
using System.IdentityModel.Tokens.Jwt;
using IBSMobile.Contracts;
using IBSMobile.Data;
using IBSMobile.Functions;
using IBSMobile.Services;
using IBSMobile.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TenantDatabaseOptions>(
    builder.Configuration.GetSection(TenantDatabaseOptions.SectionName));

builder.Services.AddSingleton<ICompanyConnectionResolver, CompanyConnectionResolver>();
builder.Services.AddScoped<ITenantConnectionAccessor, TenantConnectionAccessor>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var tenant = sp.GetRequiredService<ITenantConnectionAccessor>();
    options.UseSqlServer(tenant.ConnectionString);
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<IBSFunctions>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubscriberService, SubscriberService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "cRun2thOqorIcT6ibufXs6cRa1raTrur";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.MapInboundClaims = false;

        // Use classic handler — matches AuthService token creation and avoids IDX14102 with JsonWebTokenHandler
        options.TokenHandlers.Clear();
        options.TokenHandlers.Add(new JwtSecurityTokenHandler
        {
            MapInboundClaims = false,
        });

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "IBS",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "IBS",
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IBS Mobile API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IBS Mobile API v1");
    });
}

app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
