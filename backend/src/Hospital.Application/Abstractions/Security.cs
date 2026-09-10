using Hospital.Domain.Entities;

namespace Hospital.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) Create(User user);
}

public interface IUhidGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
