using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PokedexApi.Data;
using PokedexApi.Helpers;
using PokedexApi.Middleware;
using PokedexApi.Services;
using PokedexApi.Hubs;

namespace PokedexApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // CORS - PHẢI TRƯỚC Authentication
            services.AddCorsPolicy(Configuration);

            // JWT Authentication với xử lý OPTIONS
            services.AddJwtAuthentication(Configuration);

            // Services - Dependency Injection
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IBattleService, BattleService>();

            // Background Services
            services.AddHostedService<BattleCleanupService>();

            // Health Checks
            services.AddHealthChecks();

            // SignalR for real-time battles
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 102400; // 100 KB
                options.StreamBufferCapacity = 10;
            });

            // Controllers
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                });

            // Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Pokedex API",
                    Version = "v1",
                    Description = "Pokemon Team Builder & Battle System API"
                });

                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pokedex API v1");
                    c.RoutePrefix = string.Empty; // Swagger at root
                });
            }

            app.Use(async (context, next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:4200");
                    context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                    context.Response.Headers.Add("Access-Control-Max-Age", "86400");
                    await context.Response.CompleteAsync();
                    return;
                }
                await next();
            });

            // 2. Custom Middleware
            app.UseMiddleware<ErrorHandlerMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();

            // 3. TRÁNH HTTPS Redirect trong development (gây lỗi CORS)
            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            // 4. CORS - PHẢI TRƯỚC Authentication
            app.UseCors(CorsConfiguration.PolicyName);

            // 5. Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // 6. Endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                // SignalR Hub for real-time battles
                endpoints.MapHub<BattleHub>("/hubs/battle");

                // Health check endpoint
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}