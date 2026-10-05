using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

/// <summary>
/// FCM (or other push) registration token for one signed-in patient's device.
/// The token is a credential: do not log it.
/// </summary>
public class PatientDeviceToken : BaseEntity
{
    public const int TokenMaxLength = 4096;
    public const int PlatformMaxLength = 16;

    public const string Android = "android";
    public const string Ios = "ios";
    public const string Web = "web";

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;

    public static bool IsKnownPlatform(string? platform) =>
        platform is Android or Ios or Web;
}
