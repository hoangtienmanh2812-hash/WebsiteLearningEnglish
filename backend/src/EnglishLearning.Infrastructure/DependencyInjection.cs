using EnglishLearning.Application.Features.Auth.Interfaces;
using EnglishLearning.Application.Features.Auth.Services;
using EnglishLearning.Application.Interfaces;
using EnglishLearning.Infrastructure.Authentication;
using EnglishLearning.Infrastructure.Data;
using EnglishLearning.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
            services.AddSingleton<IJwtProvider, JwtProvider>();

            // Cấu hình JWT Authentication
            var secretKey = configuration["Jwt:Secret"] ?? "DayLaMotSecretKeyRatDaiVaBaoMatChoJWT!@#123";
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
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
