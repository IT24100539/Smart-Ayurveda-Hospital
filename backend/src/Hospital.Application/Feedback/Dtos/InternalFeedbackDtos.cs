using Hospital.Domain.Enums;

namespace Hospital.Application.Communication.Dtos;

/// <summary>
/// Comment, rating, and patient context for the internal feedback-support agent.
/// Staff JWT is not used; the API key guard on the controller is the only caller.
/// </summary>
public sealed record InternalFeedbackContextDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string? Uhid,
    string? Prakriti,
    string? Vikriti,
    int Rating,
    string Comment,
    bool IsAnonymous,
    Guid? AppointmentId,
    Guid? TreatmentId);

public sealed record SimilarFeedbackCountDto(
    int Count,
    FeedbackCategory Category,
    Guid ExcludePatientId);
