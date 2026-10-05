namespace Hospital.Infrastructure.Doctors;

public sealed class DoctorPhotoOptions
{
    public const string SectionName = "DoctorPhotos";

    /// <summary>512 KB keeps a portrait under the API's 1 MB request cap, including multipart overhead.</summary>
    public const int DefaultMaxBytes = 512 * 1024;

    public string StoragePath { get; set; } = string.Empty;
    public int MaxBytes { get; set; } = DefaultMaxBytes;
}
