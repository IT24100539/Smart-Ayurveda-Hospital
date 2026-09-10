using Hospital.Application.Abstractions;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Identity;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class SequentialUhidGenerator : IUhidGenerator
{
    private readonly HospitalDbContext _db;

    public SequentialUhidGenerator(HospitalDbContext db) => _db = db;

    public async Task<string> NextAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"SAH-{year}-";
        var last = await _db.Patients
            .Where(x => x.Uhid.StartsWith(prefix))
            .OrderByDescending(x => x.Uhid)
            .Select(x => x.Uhid)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n))
        {
            next = n + 1;
        }

        return $"{prefix}{next:D5}";
    }
}
