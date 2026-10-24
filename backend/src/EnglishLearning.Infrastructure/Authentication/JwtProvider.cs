using EnglishLearning.Application.Features.Auth.Interfaces;
using EnglishLearning.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace EnglishLearning.Infrastructure.Authentication
{
    public class JwtProvider : IJwtProvider
    {
        private readonly IConfiguration _configuration;

        public JwtProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Generate(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.FullName)
            };

            if (IsAdmin(user))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var secretKey = _configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Secret.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "EnglishLearningAPI",
                audience: _configuration["Jwt:Audience"] ?? "EnglishLearningClient",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool IsAdmin(User user)
        {
            if (user.IsAdmin)
            {
                return true;
            }

            var configuredEmails = _configuration
                .GetSection("GoogleDrive:AdminEmails")
                .Get<string[]>() ?? Array.Empty<string>();

            return configuredEmails.Any(email =>
                !string.IsNullOrWhiteSpace(email) &&
                string.Equals(email.Trim(), user.Email, StringComparison.OrdinalIgnoreCase));
        }

    }
}
