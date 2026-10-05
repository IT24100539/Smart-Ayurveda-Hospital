namespace Hospital.Infrastructure.Documents;

public sealed class MedicalDocumentOptions
{
    public const string SectionName = "MedicalDocuments";

    /// <summary>10 MB. Lab reports and scan images stay under the API body ceiling.</summary>
    public const int DefaultMaxBytes = 10 * 1024 * 1024;

    public const int AbsoluteMaxBytes = 25 * 1024 * 1024;
    public const int MultipartOverheadBytes = 64 * 1024;
    public const long RequestBytesCeiling = AbsoluteMaxBytes + MultipartOverheadBytes;

    public string StoragePath { get; set; } = string.Empty;
    public int MaxBytes { get; set; } = DefaultMaxBytes;

    public static int NormalizeMaxBytes(int configured) =>
        configured < 1024 || configured > AbsoluteMaxBytes ? DefaultMaxBytes : configured;
}
