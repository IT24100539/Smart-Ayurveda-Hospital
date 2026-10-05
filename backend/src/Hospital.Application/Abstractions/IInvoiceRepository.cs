using Hospital.Domain.Entities;

namespace Hospital.Application.Abstractions;

public interface IInvoiceRepository
{
    Task<Invoice?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Invoice> Items, int Total)> ListAsync(
        Guid? patientId,
        InvoiceStatus? status,
        bool patientVisibleOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> IsSourceOpenAsync(string openSourceKey, Guid? exceptInvoiceId, CancellationToken cancellationToken);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);

    Task AddPaymentAsync(InvoicePayment payment, CancellationToken cancellationToken);

    void ReplaceLines(Invoice invoice, IReadOnlyList<InvoiceLine> lines);
}
