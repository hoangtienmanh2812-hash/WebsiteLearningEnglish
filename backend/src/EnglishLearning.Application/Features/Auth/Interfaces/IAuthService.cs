using EnglishLearning.Application.Features.Auth.DTOs;

namespace EnglishLearning.Application.Features.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}

