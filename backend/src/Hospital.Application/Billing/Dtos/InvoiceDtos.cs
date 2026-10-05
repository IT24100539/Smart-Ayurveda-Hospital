using Hospital.Domain.Entities;

namespace Hospital.Application.Billing.Dtos;

public sealed record InvoiceLineRequest(
    Guid? AppointmentId,
    Guid? AdmissionId,
    int Quantity,
    decimal? UnitPrice);

public sealed record CreateInvoiceRequest(
    Guid PatientId,
    string? Currency,
    string? Notes,
    IReadOnlyList<InvoiceLineRequest> Lines);

public sealed record UpdateInvoiceRequest(
    string? Currency,
    string? Notes,
    IReadOnlyList<InvoiceLineRequest> Lines);

public sealed record RecordPaymentRequest(
    decimal Amount,
    string Method,
    DateTimeOffset PaidOn,
    string? Reference);

public sealed record InvoiceLineDto(
    Guid Id,
    InvoiceLineSource Source,
    Guid? AppointmentId,
    Guid? TreatmentId,
    Guid? AdmissionId,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record InvoicePaymentDto(
    Guid Id,
    decimal Amount,
    InvoicePaymentMethod Method,
    DateTimeOffset PaidOn,
    string? Reference,
    Guid RecordedByUserId);

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid PatientId,
    string Currency,
    InvoiceStatus Status,
    decimal Total,
    decimal AmountPaid,
    decimal Balance,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? CancelledAt,
    string? Notes,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<InvoicePaymentDto> Payments);
