using Hospital.Application.Abstractions;
using Hospital.Application.Auth;
using Hospital.Application.Auth.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_OpensAPatientRecordForTheSameEmail()
    {
        var users = new FakeUsers();
        var patients = new FakePatients();
        var sut = CreateSut(users, patients);

        var result = await sut.RegisterAsync(NewRequest("Nimal Silva"), actorRole: null, CancellationToken.None);

        Assert.Equal(UserRole.Patient, result.User.Role);
        var patient = Assert.Single(patients.Items);
        Assert.Equal("nimal@example.com", patient.Email);
        Assert.Equal("Nimal", patient.FirstName);
        Assert.Equal("Silva", patient.LastName);
        Assert.Equal("0771234567", patient.Phone);
        Assert.Equal(new DateOnly(1992, 3, 4), patient.DateOfBirth);
        Assert.Equal(Gender.Male, patient.Gender);
        Assert.Equal("SAH-2026-00042", patient.Uhid);
        Assert.Equal(DoshaType.None, patient.Prakriti);
    }

    [Fact]
    public async Task RegisterAsync_WhenPhoneBelongsToAnotherPatient_ThrowsConflict()
    {
        var users = new FakeUsers();
        var patients = new FakePatients();
        patients.Items.Add(new Patient
        {
            Phone = "0771234567",
            Email = "other@example.com",
            Uhid = "SAH-2026-00001"
        });
        var sut = CreateSut(users, patients);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RegisterAsync(NewRequest("Nimal Silva"), actorRole: null, CancellationToken.None));
        Assert.Equal(AuthMessages.RegistrationUnavailable, error.Message);
        Assert.DoesNotContain("0771234567", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("SAH-2026-00001", error.Message, StringComparison.Ordinal);
        Assert.Empty(users.Items);
        Assert.Single(patients.Items);
    }

    [Fact]
    public async Task RegisterAsync_LinksAnExistingChartThatHasNoEmail()
    {
        var patients = new FakePatients();
        var existing = new Patient
        {
            Phone = "0771234567",
            Email = null,
            Uhid = "SAH-2026-00001",
            FirstName = "Nimal"
        };
        patients.Items.Add(existing);
        var sut = CreateSut(new FakeUsers(), patients);

        await sut.RegisterAsync(NewRequest("Nimal Silva"), actorRole: null, CancellationToken.None);

        Assert.Equal("nimal@example.com", existing.Email);
        Assert.Single(patients.Items);
    }

    [Fact]
    public async Task RegisterAsync_MatchesAnExistingChartIgnoringCaseAndWhitespace()
    {
        var patients = new FakePatients();
        var existing = new Patient
        {
            Phone = "0110000000",
            Email = "  Nimal@Example.com ",
            Uhid = "SAH-2026-00007",
            FirstName = "Nimal"
        };
        patients.Items.Add(existing);
        var sut = CreateSut(new FakeUsers(), patients);

        await sut.RegisterAsync(NewRequest("Nimal Silva"), actorRole: null, CancellationToken.None);

        Assert.Equal("nimal@example.com", existing.Email);
        Assert.Single(patients.Items);
    }

    [Fact]
    public async Task RequestReset_UnknownEmail_ReturnsTheSameMessageAndDoesNotSend()
    {
        var email = new FakeEmailSender();
        var sms = new FakeSmsSender();
        var sut = CreateResetSut(new FakeUsers(), email, sms);

        var known = await sut.RequestResetAsync(
            new ForgotPasswordRequest("missing@example.com"),
            "https://app.local/reset-password",
            CancellationToken.None);
        var unknown = await sut.RequestResetAsync(
            new ForgotPasswordRequest("also-missing@example.com"),
            "https://app.local/reset-password",
            CancellationToken.None);

        Assert.Equal(known.Message, unknown.Message);
        Assert.Contains("If an account with that email exists", known.Message);
        Assert.Equal(0, email.Calls);
        Assert.Equal(0, sms.Calls);
    }

    [Fact]
    public async Task RequestReset_KnownEmail_StoresHashedTokenAndSendsDevLink()
    {
        var users = new FakeUsers();
        var user = SeedResetUser(users);
        var email = new FakeEmailSender();
        var sms = new FakeSmsSender();
        var sut = CreateResetSut(users, email, sms);

        var response = await sut.RequestResetAsync(
            new ForgotPasswordRequest("RESET@Example.com"),
            "https://app.local/reset-password",
            CancellationToken.None);

        Assert.Contains("If an account with that email exists", response.Message);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordResetTokenHash));
        Assert.NotEqual(email.LastToken, user.PasswordResetTokenHash);
        Assert.Equal(HashToken(email.LastToken!), user.PasswordResetTokenHash);
        Assert.Equal(1, email.Calls);
        Assert.Equal(1, sms.Calls);
        Assert.Contains(email.LastToken!, sms.LastMessage);
    }

    [Fact]
    public async Task CompleteReset_Success_RevokesTokenAndIncrementsTokenVersion()
    {
        var users = new FakeUsers();
        var user = SeedResetUser(users, tokenVersion: 3);
        var email = new FakeEmailSender();
        var sut = CreateResetSut(users, email, new FakeSmsSender());

        await sut.RequestResetAsync(
            new ForgotPasswordRequest(user.Email),
            "https://app.local/reset-password",
            CancellationToken.None);

        var result = await sut.CompleteResetAsync(
            new ResetPasswordRequest(user.Email, email.LastToken!, "NewPass!2345", "NewPass!2345"),
            CancellationToken.None);

        Assert.Contains("reset successfully", result.Message);
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpiresAt);
        Assert.Equal(4, user.TokenVersion);
        Assert.Equal("hashed:NewPass!2345", user.PasswordHash);
    }

    [Fact]
    public async Task CompleteReset_ExpiredToken_IsRejectedAndRevoked()
    {
        var users = new FakeUsers();
        var user = SeedResetUser(users);
        var email = new FakeEmailSender();
        var clock = new FakeClock(DateTimeOffset.Parse("2026-10-02T08:00:00Z"));
        var sut = CreateResetSut(users, email, new FakeSmsSender(), clock);

        await sut.RequestResetAsync(
            new ForgotPasswordRequest(user.Email),
            "https://app.local/reset-password",
            CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddMinutes(31);

        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            sut.CompleteResetAsync(
                new ResetPasswordRequest(user.Email, email.LastToken!, "NewPass!2345", "NewPass!2345"),
                CancellationToken.None));

        Assert.Contains("Invalid or expired", error.Message);
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Equal(1, user.TokenVersion);
        Assert.Equal("hashed:old-password", user.PasswordHash);
    }

    [Fact]
    public async Task CompleteReset_ReusedToken_IsRejected()
    {
        var users = new FakeUsers();
        var user = SeedResetUser(users);
        var email = new FakeEmailSender();
        var sut = CreateResetSut(users, email, new FakeSmsSender());

        await sut.RequestResetAsync(
            new ForgotPasswordRequest(user.Email),
            "https://app.local/reset-password",
            CancellationToken.None);
        var token = email.LastToken!;

        await sut.CompleteResetAsync(
            new ResetPasswordRequest(user.Email, token, "NewPass!2345", "NewPass!2345"),
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            sut.CompleteResetAsync(
                new ResetPasswordRequest(user.Email, token, "OtherPass!2345", "OtherPass!2345"),
                CancellationToken.None));

        Assert.Contains("Invalid or expired", error.Message);
        Assert.Equal("hashed:NewPass!2345", user.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_DoesNotRevealTheEmail()
    {
        var users = new FakeUsers();
        users.Items.Add(new User
        {
            Email = "nimal@example.com",
            PhoneNumber = "0110000000",
            PasswordHash = "hashed",
            IsActive = true
        });
        var sut = CreateSut(users, new FakePatients());

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RegisterAsync(NewRequest("Nimal Silva"), actorRole: null, CancellationToken.None));

        Assert.Equal(AuthMessages.RegistrationUnavailable, error.Message);
        Assert.DoesNotContain("nimal@example.com", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("already exists", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_LocksAfterConfiguredFailures_AndUnlocksWhenTheClockPassesTheWindow()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero));
        var users = new FakeUsers();
        var user = new User
        {
            Email = "lock@example.com",
            PhoneNumber = "0771234567",
            PasswordHash = "hashed:secret",
            Role = UserRole.Patient,
            IsActive = true
        };
        users.Items.Add(user);
        var sut = new AuthService(
            users,
            new FakePatients(),
            new FakeUhid(),
            new RecordingHasher(),
            new FakeJwt(),
            new FakeUnitOfWork(),
            emailSender: null,
            smsSender: null,
            clock: clock,
            lockout: new AccountLockoutOptions { MaxFailedAttempts = 3, LockoutMinutes = 15 });

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var failed = await Assert.ThrowsAsync<UnauthorizedException>(() =>
                sut.LoginAsync(new LoginRequest(user.Email, "wrong"), CancellationToken.None));
            Assert.Equal(AuthMessages.InvalidCredentials, failed.Message);
        }

        Assert.NotNull(user.LockoutEnd);

        var locked = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.LoginAsync(new LoginRequest(user.Email, "secret"), CancellationToken.None));
        Assert.Equal(AuthMessages.AccountLocked, locked.Message);
        Assert.DoesNotContain(user.Email, locked.Message, StringComparison.OrdinalIgnoreCase);

        clock.UtcNow = clock.UtcNow.AddMinutes(15);
        var unlocked = await sut.LoginAsync(new LoginRequest(user.Email, "secret"), CancellationToken.None);
        Assert.Equal(user.Email, unlocked.User.Email);
        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockoutEnd);
    }

    [Fact]
    public async Task Login_UnknownEmail_DoesNotRevealWhetherTheAccountExists()
    {
        var sut = CreateSut(new FakeUsers(), new FakePatients());
        var error = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.LoginAsync(new LoginRequest("missing@example.com", "WrongPass!111"), CancellationToken.None));

        Assert.Equal(AuthMessages.InvalidCredentials, error.Message);
        Assert.DoesNotContain("missing@example.com", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_WithoutAUnitOfWork_RejectsAndDoesNotOpenAChart()
    {
        var users = new FakeUsers();
        var patients = new FakePatients();
        var sut = new AuthService(users, patients, new FakeUhid(), new FakeHasher(), new FakeJwt());

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            sut.RegisterAsync(NewRequest("Nimal Silva"), actorRole: null, CancellationToken.None));

        Assert.Contains("No patient record was opened", error.Message);
        Assert.Empty(users.Items);
        Assert.Empty(patients.Items);
    }

    private static AuthService CreateSut(FakeUsers users, FakePatients patients) =>
        new(users, patients, new FakeUhid(), new FakeHasher(), new FakeJwt(), new FakeUnitOfWork());

    private static AuthService CreateResetSut(
        FakeUsers users,
        FakeEmailSender email,
        FakeSmsSender sms,
        FakeClock? clock = null) =>
        new(
            users,
            new FakePatients(),
            new FakeUhid(),
            new RecordingHasher(),
            new FakeJwt(),
            new FakeUnitOfWork(),
            email,
            sms,
            clock);

    private static User SeedResetUser(FakeUsers users, int tokenVersion = 1)
    {
        var user = new User
        {
            Email = "reset@example.com",
            PhoneNumber = "0771234567",
            PasswordHash = "hashed:old-password",
            Role = UserRole.Patient,
            IsActive = true,
            TokenVersion = tokenVersion
        };
        users.Items.Add(user);
        return user;
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static RegisterRequest NewRequest(string fullName) => new(
        fullName,
        "nimal@example.com",
        "0771234567",
        "ChangeMe!Patient1",
        DateOfBirth: new DateOnly(1992, 3, 4),
        Gender: Gender.Male);

    private sealed class FakeUhid : IUhidGenerator
    {
        public Task<string> NextAsync(CancellationToken cancellationToken) => Task.FromResult("SAH-2026-00042");
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "hashed";
        public bool Verify(string password, string hash) => hash == "hashed";
    }

    private sealed class RecordingHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; set; }
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public int Calls { get; private set; }
        public string? LastToken { get; private set; }

        public Task SendPasswordResetEmailAsync(
            string toEmail,
            string resetToken,
            string resetUrl,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastToken = resetToken;
            return Task.CompletedTask;
        }

        public Task SendEmailAsync(
            string toEmail,
            string subject,
            string body,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeSmsSender : ISmsSender
    {
        public int Calls { get; private set; }
        public string? LastMessage { get; private set; }

        public Task SendSmsAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastMessage = message;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public (string Token, DateTimeOffset ExpiresAt) Create(User user) =>
            ("token", DateTimeOffset.UtcNow.AddHours(1));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeUsers : IUserRepository
    {
        public List<User> Items { get; } = new();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                string.Equals(x.Email.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task<User?> FindActiveByRoleAsync(UserRole role, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Role == role && x.IsActive));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Items.Add(user);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<User>> ListStaffAsync(string? query, UserRole? role, bool? isActive, int skip, int take, CancellationToken cancellationToken)
        {
            var q = Items.Where(x => x.Role != UserRole.Patient);
            if (role.HasValue) q = q.Where(x => x.Role == role.Value);
            if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive.Value);
            IReadOnlyList<User> list = q.Skip(skip).Take(take).ToList();
            return Task.FromResult(list);
        }

        public Task<int> CountStaffAsync(string? query, UserRole? role, bool? isActive, CancellationToken cancellationToken)
        {
            var q = Items.Where(x => x.Role != UserRole.Patient);
            if (role.HasValue) q = q.Where(x => x.Role == role.Value);
            if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive.Value);
            return Task.FromResult(q.Count());
        }

        public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Items.Count(x => x.Role == UserRole.Admin && x.IsActive));
    }

    private sealed class FakePatients : IPatientRepository
    {
        public List<Patient> Items { get; } = new();

        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<Patient?> GetByPhoneAsync(string phone, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                string.Equals(x.Phone.Trim(), phone.Trim(), StringComparison.Ordinal)));

        public Task<Patient?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                string.Equals(x.Email?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task<(IReadOnlyList<Patient> Items, int Total)> SearchAsync(
            string? query, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Patient>)Items, Items.Count));

        public Task AddAsync(Patient patient, CancellationToken cancellationToken)
        {
            Items.Add(patient);
            return Task.CompletedTask;
        }
    }
}
