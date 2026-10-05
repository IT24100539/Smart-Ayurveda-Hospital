using System.Text.Json.Serialization;

namespace Hospital.Application.Doctors.Dtos;

public sealed record CreateDoctorRequest(
    string Name,
    string Specialty,
    string Qualifications,
    string? Bio);

public sealed record UpdateDoctorRequest(
    string Name,
    string Specialty,
    string Qualifications,
    string? Bio);

public sealed class DoctorPhotoContent
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
}

public sealed class DoctorPhotoFile
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
}

public sealed record DoctorDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Specialty { get; init; }
    public required string Qualifications { get; init; }
    public string? Bio { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsSample { get; init; }
    public required bool HasPhoto { get; init; }

    /// <summary>Authorized photo route. Null when the physician has no portrait.</summary>
    public string? PhotoUrl { get; init; }

    /// <summary>
    /// Average of visible feedback on completed appointments for this physician.
    /// Omitted when no such feedback exists.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Rating { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RatingCount { get; init; }
}
