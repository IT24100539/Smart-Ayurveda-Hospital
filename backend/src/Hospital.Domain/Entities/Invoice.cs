using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

/// <summary>
/// A bill raised by staff from a real treatment visit or an approved ward admission.
/// The desk records cash, card, or transfer by hand. There is no payment gateway.
/// </summary>
public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Paid = 3,
    Cancelled = 4
}

public enum InvoiceLineSource
{
    Treatment = 1,
    Admission = 2
}

public enum InvoicePaymentMethod
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3
}

public sealed class Invoice : BaseEntity
{
    public const int NumberMaxLength = 32;
    public const int CurrencyLength = 3;
    public const string DefaultCurrency = "LKR";
    public const int NotesMaxLength = 500;

    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string Currency { get; set; } = DefaultCurrency;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? Notes { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();
    public List<InvoicePayment> Payments { get; set; } = new();

    public decimal Balance => Total - AmountPaid;
}

public sealed class InvoiceLine : BaseEntity
{
    public const int DescriptionMaxLength = 240;
    public const int OpenSourceKeyMaxLength = 80;
    public const int MaxLines = 30;
    public const int MaxQuantity = 366;
    public const decimal MaxUnitPrice = 1_000_000m;
    public const decimal MaxTotal = 9_999_999_999.99m;

    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public int SortOrder { get; set; }
    public InvoiceLineSource Source { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Set while the invoice is still a live bill. Cleared on cancel so the visit or stay can be billed again.
    /// </summary>
    public string? OpenSourceKey { get; set; }

    public Guid? TreatmentId { get; set; }
    public Treatment? Treatment { get; set; }

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid? AdmissionRequestId { get; set; }
    public AdmissionRequest? AdmissionRequest { get; set; }

    public static bool HasAtMostTwoDecimals(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero) == value;

    public static decimal CalculateLineTotal(int quantity, decimal unitPrice) =>
        decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);

    public static string TreatmentKey(Guid appointmentId) => $"appointment:{appointmentId:D}";

    public static string AdmissionKey(Guid admissionRequestId) => $"admission:{admissionRequestId:D}";
}

public sealed class InvoicePayment : BaseEntity
{
    public const int ReferenceMaxLength = 80;

    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public decimal Amount { get; set; }
    public InvoicePaymentMethod Method { get; set; }
    public DateTimeOffset PaidOn { get; set; }
    public string? Reference { get; set; }
    public Guid RecordedByUserId { get; set; }
}
