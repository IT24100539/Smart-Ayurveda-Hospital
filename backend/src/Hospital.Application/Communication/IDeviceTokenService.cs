using Hospital.Application.Communication.Dtos;

namespace Hospital.Application.Communication;

public interface IDeviceTokenService
{
    /// <summary>
    /// Stores a push token for the signed-in patient chart.
    /// The chart id comes from the login, never from the request body.
    /// </summary>
    Task<DeviceTokenDto> RegisterAsync(RegisterDeviceTokenRequest request, CancellationToken cancellationToken);
}
