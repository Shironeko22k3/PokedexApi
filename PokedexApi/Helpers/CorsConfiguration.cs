using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace PokedexApi.Helpers
{
    public static class CorsConfiguration
    {
        public const string PolicyName = "AllowAngularApp";

        public static void AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200" };

            services.AddCors(options =>
            {
                options.AddPolicy(PolicyName, builder =>
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials()
                           .SetPreflightMaxAge(TimeSpan.FromHours(24)); // Cache preflight cho 24 giờ
                });

                // Thêm policy mặc định cho OPTIONS requests
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            });
        }
    }
}