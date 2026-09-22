using EnglishLearning.Domain.Entities;

namespace EnglishLearning.Application.Features.Auth.Interfaces
{
    public interface IJwtProvider
    {
        string Generate(User user);
    }
}

