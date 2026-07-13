using IBSMobile.Data;
using IBSMobile.Statics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IBSMobile.Services
{
    public static class ApiServerUriResolver
    {
        public static string Resolve(ApplicationDbContext context, IConfiguration configuration)
        {
            var selected = context.ApiServerSettings
                .AsNoTracking()
                .Select(x => x.SelectedServer)
                .FirstOrDefault();

            var server = ApiServerNames.Normalize(selected);
            var configKey = server == ApiServerNames.Neptune
                ? "ApiSettings:BaseUri2"
                : "ApiSettings:BaseUri";

            var baseUri = configuration[configKey];
            if (string.IsNullOrWhiteSpace(baseUri))
                throw new InvalidOperationException($"{configKey} is not configured.");

            return baseUri;
        }
    }
}
