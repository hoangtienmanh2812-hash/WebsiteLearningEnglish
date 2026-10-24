using EnglishLearning.Application.Features.Auth.Interfaces;
using EnglishLearning.Application.Features.Auth.Services;
using EnglishLearning.Application.Features.Admin.Interfaces;
using EnglishLearning.Application.Interfaces;
using EnglishLearning.Infrastructure.Authentication;
using EnglishLearning.Infrastructure.Data;
using EnglishLearning.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnglishLearning.Infrastructure.GoogleDrive;
namespace EnglishLearning.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Cấu hình EF Core
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            // Đăng ký Repositories & Services
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISystemSettingsService, SystemSettingsService>();
            services.AddSingleton<IJwtProvider, JwtProvider>();
            // Gg drive
            services.AddScoped<GoogleDriveService>();
            services.AddScoped<IAdminLessonService>(provider =>
                provider.GetRequiredService<GoogleDriveService>());
            // Cấu hình JWT Authentication
            var secretKey = configuration["Jwt:Secret"];
            if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey) < 32)
            {
                throw new InvalidOperationException(
                    "Cần cấu hình Jwt:Secret có ít nhất 32 byte qua user-secrets hoặc biến môi trường.");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value
                                ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
                            var isConfiguredAdmin = !string.IsNullOrWhiteSpace(email) &&
                                configuration.GetSection("GoogleDrive:AdminEmails")
                                    .Get<string[]>()?
                                    .Any(configuredEmail =>
                                        !string.IsNullOrWhiteSpace(configuredEmail) &&
                                        string.Equals(
                                            configuredEmail.Trim(),
                                            email.Trim(),
                                            StringComparison.OrdinalIgnoreCase)) == true;

                            if (isConfiguredAdmin &&
                                context.Principal?.Identity is ClaimsIdentity identity)
                            {
                                if (!identity.HasClaim(ClaimTypes.Role, "Admin"))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                                }
                            }

                            return Task.CompletedTask;
                        }
                    };

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        RoleClaimType = ClaimTypes.Role,
                        ValidIssuer = configuration["Jwt:Issuer"] ?? "EnglishLearningAPI",
                        ValidAudience = configuration["Jwt:Audience"] ?? "EnglishLearningClient",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
