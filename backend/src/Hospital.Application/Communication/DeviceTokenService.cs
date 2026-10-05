using Hospital.Application.Abstractions;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Communication;

public sealed class DeviceTokenService : IDeviceTokenService
{
    private readonly IPatientDeviceTokenRepository _tokens;
    private readonly IActorContext _actors;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeviceTokenService(
        IPatientDeviceTokenRepository tokens,
        IActorContext actors,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _tokens = tokens;
        _actors = actors;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<DeviceTokenDto> RegisterAsync(RegisterDeviceTokenRequest request, CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var token = request.Token?.Trim() ?? string.Empty;
        var platform = request.Platform?.Trim().ToLowerInvariant() ?? string.Empty;
        if (token.Length is 0 or > PatientDeviceToken.TokenMaxLength)
        {
            throw new DomainException("Enter the device token from this phone.");
        }

        if (!PatientDeviceToken.IsKnownPlatform(platform))
        {
            throw new DomainException("Device platform must be android, ios, or web.");
        }

        var now = _clock.UtcNow;
        var existing = await _tokens.FindByTokenAsync(token, cancellationToken);
        if (existing is null)
        {
            existing = new PatientDeviceToken
            {
                PatientId = patient.Id,
                Token = token,
                Platform = platform,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _tokens.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.PatientId = patient.Id;
            existing.Platform = platform;
            existing.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new DeviceTokenDto(existing.Id, existing.Platform, existing.UpdatedAt);
    }
}
