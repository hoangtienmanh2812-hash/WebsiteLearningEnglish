using EnglishLearning.Application.Features.Auth.DTOs;
using EnglishLearning.Application.Features.Auth.Interfaces;
using EnglishLearning.Application.Interfaces;
using EnglishLearning.Domain.Entities;
using System;
using System.Threading.Tasks;
using BCrypt.Net;

namespace EnglishLearning.Application.Features.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtProvider _jwtProvider;

        public AuthService(IUserRepository userRepository, IJwtProvider jwtProvider)
        {
            _userRepository = userRepository;
            _jwtProvider = jwtProvider;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new Exception("Email đã được sử dụng.");
            }

            // Tạo user mới
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            // Generate token
            var token = _jwtProvider.Generate(user);

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Token = token
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Email hoặc mật khẩu không chính xác.");
            }

            var token = _jwtProvider.Generate(user);

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Token = token
            };
        }
    }
}

