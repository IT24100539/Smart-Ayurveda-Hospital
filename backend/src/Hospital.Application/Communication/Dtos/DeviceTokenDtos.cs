namespace Hospital.Application.Communication.Dtos;

public sealed record RegisterDeviceTokenRequest(string Token, string Platform);

public sealed record DeviceTokenDto(Guid Id, string Platform, DateTimeOffset RegisteredAt);
